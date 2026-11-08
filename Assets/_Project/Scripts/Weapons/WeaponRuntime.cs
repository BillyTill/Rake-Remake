using System.Collections;
using UnityEngine;
using Lake.Core;
using Lake.Aim;
using Lake.Weapons.VFX;

namespace Lake.Weapons
{
    /// <summary>
    /// Один экземпляр оружия. Raycast от muzzlePoint.position в aimPivot.forward.
    /// Учитывает spread, falloff, hit zones, split recoil.
    /// </summary>
    public class WeaponRuntime : MonoBehaviour
    {
        [SerializeField] WeaponDataSO data;
        [SerializeField] Transform muzzlePoint;
        [SerializeField] LineRenderer tracerPrefab;
        [SerializeField] float tracerDuration = 0.08f;
        [SerializeField] LayerMask muzzleBlockMask = ~0;

        [Header("VFX & Audio")]
        [SerializeField] WeaponAnimator animator;
        [SerializeField] ShellEjector shellEjector;
        [SerializeField] WeaponAudio weaponAudio;

        // Assigned by WeaponController at runtime
        [System.NonSerialized] public AimPivot aimPivot;
        [System.NonSerialized] public RecoilController recoil;

        public WeaponDataSO Data => data;
        public int CurrentAmmo { get; private set; }
        public int ReserveAmmo { get; private set; }
        public bool IsReloading { get; private set; }

        public event System.Action<int, int> OnAmmoChanged;

        InputProvider _input;
        float _nextFireTime;
        float _currentSpread;
        Coroutine _reloadRoutine;
        Coroutine _fireRoutine;
        bool _isAds;
        bool _initialized;
        bool _subscribed;

        public bool IsAds => _isAds;

        public void Initialize()
        {
            _input = InputProvider.Instance;
            CurrentAmmo = data.magazineSize;
            ReserveAmmo = data.reserveAmmoMax;
            OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);

            _initialized = true;
            Subscribe();
        }

        void OnEnable()
        {
            if (_initialized) Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();

            if (_fireRoutine != null) { StopCoroutine(_fireRoutine); _fireRoutine = null; }
            if (_reloadRoutine != null) { StopCoroutine(_reloadRoutine); _reloadRoutine = null; }
            IsReloading = false;
        }

        void OnDestroy() => Unsubscribe();

        void Subscribe()
        {
            if (_input == null || _subscribed) return;
            _input.FireStarted += OnFirePress;
            _input.FireStopped += OnFireRelease;
            _input.ReloadPressed += OnReloadPress;
            _input.ADSStarted += OnAdsStart;
            _input.ADSEnded += OnAdsEnd;
            _subscribed = true;
        }

        void Unsubscribe()
        {
            if (_input == null || !_subscribed) return;
            _input.FireStarted -= OnFirePress;
            _input.FireStopped -= OnFireRelease;
            _input.ReloadPressed -= OnReloadPress;
            _input.ADSStarted -= OnAdsStart;
            _input.ADSEnded -= OnAdsEnd;
            _subscribed = false;
        }

        void OnAdsStart() => _isAds = true;
        void OnAdsEnd() => _isAds = false;

        void OnFirePress()
        {
            if (data.isAutomatic)
            {
                if (_fireRoutine == null) _fireRoutine = StartCoroutine(AutoFireLoop());
            }
            else TryFire();
        }

        void OnFireRelease()
        {
            if (_fireRoutine != null)
            {
                StopCoroutine(_fireRoutine);
                _fireRoutine = null;
            }
        }

        IEnumerator AutoFireLoop()
        {
            while (_input != null && _input.FireHeld)
            {
                TryFire();
                yield return new WaitForSeconds(data.fireRate);
            }
            _fireRoutine = null;
        }

        void OnReloadPress()
        {
            if (IsReloading || CurrentAmmo >= data.magazineSize || ReserveAmmo <= 0) return;
            if (data.shellByShellReload) _reloadRoutine = StartCoroutine(ShellReloadRoutine());
            else _reloadRoutine = StartCoroutine(BulkReloadRoutine());
        }

        IEnumerator BulkReloadRoutine()
        {
            IsReloading = true;
            yield return new WaitForSeconds(data.reloadTime);
            int need = data.magazineSize - CurrentAmmo;
            int take = Mathf.Min(need, ReserveAmmo);
            CurrentAmmo += take;
            ReserveAmmo -= take;
            OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);
            IsReloading = false;
        }

        IEnumerator ShellReloadRoutine()
        {
            IsReloading = true;
            while (CurrentAmmo < data.magazineSize && ReserveAmmo > 0)
            {
                yield return new WaitForSeconds(data.shellReloadTime);
                if (_input.FireHeld) { IsReloading = false; yield break; } // прервано стрельбой
                CurrentAmmo++;
                ReserveAmmo--;
                OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);
            }
            IsReloading = false;
        }

        void TryFire()
        {
            if (data == null || aimPivot == null) return;
            if (IsReloading || Time.time < _nextFireTime) return;
            if (CurrentAmmo <= 0) { weaponAudio?.PlayEmpty(); return; }

            _nextFireTime = Time.time + data.fireRate;
            CurrentAmmo--;
            OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);

            for (int i = 0; i < data.pelletsPerShot; i++) FirePellet();

            // VFX
            if (muzzlePoint != null)
                MuzzleFlashFactory.CreateMuzzleFlash(muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
            weaponAudio?.PlayFire();
            animator?.ApplyKick();
            shellEjector?.Eject();
            recoil?.ApplyRecoil(data.recoilVertical, data.recoilHorizontal);
            _currentSpread = Mathf.Min(_currentSpread + data.spreadPerShot, data.maxSpread);
        }

        void FirePellet()
        {
            Vector3 origin = muzzlePoint.position;
            Vector3 dir = ApplySpread(aimPivot.AimDirection, ComputeSpread());

            // Если дуло "в стене" — сдвигаем к aimPivot
            if (Physics.Linecast(aimPivot.AimOrigin, origin, out var block, muzzleBlockMask))
                origin = aimPivot.AimOrigin + (origin - aimPivot.AimOrigin).normalized * (block.distance * 0.9f);

            Vector3 hitPoint = origin + dir * data.range;
            if (Physics.Raycast(origin, dir, out var hit, data.range, data.hitMask))
            {
                hitPoint = hit.point;
                float falloff = data.ComputeFalloff(hit.distance);
                float dmg = data.damagePerPellet * falloff;

                var zone = hit.collider.GetComponent<HitZone>();
                if (zone != null) dmg *= zone.Multiplier;

                var dmgable = hit.collider.GetComponentInParent<IDamageable>();
                if (dmgable != null)
                    dmgable.TakeDamage(new DamageInfo(dmg, hit.point, dir, gameObject,
                        zone != null ? zone.ZoneType : HitZoneType.Body));

                // Impact VFX
                ImpactEffect.Spawn(hit.point, hit.normal, hit.collider.gameObject);
            }

            SpawnTracer(origin, hitPoint);
        }

        float ComputeSpread()
        {
            float s = data.baseSpread + _currentSpread;
            if (_isAds) s *= data.adsSpreadMultiplier;
            return s;
        }

        static Vector3 ApplySpread(Vector3 forward, float angleDeg)
        {
            if (angleDeg <= 0.01f) return forward;
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 rnd = Random.insideUnitCircle * Mathf.Tan(rad);
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
            right.Normalize();
            Vector3 up = Vector3.Cross(forward, right).normalized;
            return (forward + right * rnd.x + up * rnd.y).normalized;
        }

        void SpawnTracer(Vector3 from, Vector3 to)
        {
            if (tracerPrefab == null) return;
            var t = Instantiate(tracerPrefab);
            t.SetPosition(0, from);
            t.SetPosition(1, to);
            Destroy(t.gameObject, tracerDuration);
        }

        void Update()
        {
            _currentSpread = Mathf.MoveTowards(_currentSpread, 0f, data.spreadRecovery * Time.deltaTime);
        }
    }
}