using System.Collections;
using UnityEngine;
using Lake.Core;
using Lake.Player;
using Lake.Weapons;
using Lake.Flashlight;
using Lake.Health;
using Lake.BodyCam;
using Lake.Outline;

namespace Lake.UI
{
    /// <summary>
    /// Собирает ссылки на все UI-компоненты и связывает их событиями.
    /// Не содержит логики — только маршрутизация.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [SerializeField] CrosshairUI crosshair;
        [SerializeField] InteractionPromptUI prompt;
        [SerializeField] AmmoPanelUI ammo;
        [SerializeField] WeaponSlotsUI slots;
        [SerializeField] FlashlightUI flashlight;
        [SerializeField] HealthBarUI health;
        [SerializeField] BodyCamOverlayUI overlay;
        [SerializeField] KeyHintsUI keyHints;
        [SerializeField] DeathScreenUI deathScreen;
        [SerializeField] OutlineManager outlineManager;

        PlayerInteractor _interactor;
        WeaponController _weapons;
        Flashlight.Flashlight _flashlight;
        HealthComponent _health;
        BodyCamController _bodycam;
        PlayerSpawner _spawner;
        InputProvider _input;

        void Start() => StartCoroutine(BindRoutine());

        IEnumerator BindRoutine()
        {
            // Wait for systems to be ready
            while (_interactor == null)
            {
                _interactor = FindFirstObjectByType<PlayerInteractor>();
                if (_interactor == null) yield return null;
            }
            while (_weapons == null)
            {
                _weapons = FindFirstObjectByType<WeaponController>();
                if (_weapons == null) yield return null;
            }
            while (_flashlight == null)
            {
                _flashlight = FindFirstObjectByType<Flashlight.Flashlight>();
                if (_flashlight == null) yield return null;
            }
            while (_health == null)
            {
                _health = FindFirstObjectByType<HealthComponent>();
                if (_health == null) yield return null;
            }
            while (_bodycam == null)
            {
                _bodycam = FindFirstObjectByType<BodyCamController>();
                if (_bodycam == null) yield return null;
            }
            while (_spawner == null)
            {
                _spawner = FindFirstObjectByType<PlayerSpawner>();
                if (_spawner == null) yield return null;
            }
            while (_input == null)
            {
                _input = InputProvider.Instance;
                if (_input == null) yield return null;
            }

            Bind(_interactor, _weapons, _flashlight, _health, _bodycam, _spawner);
        }

        public void Bind(PlayerInteractor interactor, WeaponController weapons,
                         Flashlight.Flashlight fl, HealthComponent hp,
                         BodyCamController bc, PlayerSpawner spawner)
        {
            _interactor = interactor;
            _weapons = weapons;
            _flashlight = fl;
            _health = hp;
            _bodycam = bc;
            _spawner = spawner;

            // Interaction
            if (_interactor != null)
                _interactor.OnTargetChanged += OnTargetChanged;

            // Weapons
            if (_weapons != null)
            {
                _weapons.OnSlotChanged += OnSlotChanged;
                _weapons.OnSlotChanged += (slot, w) =>
                {
                    if (w != null) w.OnAmmoChanged += (cur, res) => ammo.SetAmmo(cur, res, w.Data);
                };
                // Начальный пуш
                for (int i = 0; i < 2; i++)
                    slots.SetSlotOccupied(i, _weapons.AllWeapons[i] != null);
            }

            // Flashlight
            if (_flashlight != null)
            {
                _flashlight.OnToggled += on => flashlight.SetState(on);
                _flashlight.OnBatteryChanged += lvl => flashlight.SetBattery(lvl);
                flashlight.SetState(_flashlight.IsOn);
            }

            // Health
            if (_health != null)
            {
                _health.OnHealthChanged += (cur, max) => health.SetHealth(cur, max);
                health.SetHealth(_health.Current, _health.MaxHealth);
            }

            // Bodycam
            if (_bodycam != null)
            {
                _bodycam.OnToggled += on => overlay.SetBodycamActive(on);
                overlay.SetBodycamActive(_bodycam.IsActive);
            }

            // Death
            if (_spawner != null)
            {
                _spawner.OnPlayerDied += OnPlayerDied;
                _spawner.OnPlayerSpawned += OnPlayerSpawned;
            }

            // ADS - crosshair dimming
            if (_input != null)
            {
                _input.ADSStarted += () => crosshair.SetDimmed(true);
                _input.ADSEnded += () => crosshair.SetDimmed(false);
            }

            // Синхронизация текущего состояния (если оружие уже в руках до биндинга)
            if (_weapons != null && _weapons.Current != null)
            {
                var current = _weapons.Current;
                int currentSlot = -1;
                for (int i = 0; i < _weapons.AllWeapons.Length; i++)
                    if (_weapons.AllWeapons[i] == current) { currentSlot = i; break; }

                if (currentSlot >= 0)
                {
                    OnSlotChanged(currentSlot, current);
                    current.OnAmmoChanged += (cur, res) => ammo.SetAmmo(cur, res, current.Data);
                    crosshair.SetVisible(true);
                }
            }
        }

        void OnTargetChanged(IInteractable target, string promptText)
        {
            prompt.SetTarget(target, promptText);
            outlineManager.SetTarget(target as Component);
        }

        void OnSlotChanged(int slot, WeaponRuntime weapon)
        {
            slots.SetActiveSlot(weapon != null ? slot : -1);
            crosshair.SetVisible(weapon != null);
            if (weapon != null) ammo.SetAmmo(weapon.CurrentAmmo, weapon.ReserveAmmo, weapon.Data);
        }

        void OnPlayerDied()
        {
            deathScreen.Show(2f);
            crosshair.SetVisible(false);
            prompt.SetTarget(null, null);
            outlineManager.SetTarget(null);
        }

        void OnPlayerSpawned(GameObject player)
        {
            deathScreen.Hide();
        }

        void Update()
        {
            // Пульсация crosshair на основе spread текущего оружия
            if (_weapons != null && _weapons.Current != null)
                crosshair.SetSpread(_weapons.Current.Data.baseSpread);
        }

        void OnDestroy()
        {
            if (_interactor != null) _interactor.OnTargetChanged -= OnTargetChanged;
            if (_weapons != null) _weapons.OnSlotChanged -= OnSlotChanged;
            if (_flashlight != null)
            {
                _flashlight.OnToggled -= on => flashlight.SetState(on);
                _flashlight.OnBatteryChanged -= lvl => flashlight.SetBattery(lvl);
            }
            if (_health != null) _health.OnHealthChanged -= (cur, max) => health.SetHealth(cur, max);
            if (_bodycam != null) _bodycam.OnToggled -= on => overlay.SetBodycamActive(on);
            if (_spawner != null)
            {
                _spawner.OnPlayerDied -= OnPlayerDied;
                _spawner.OnPlayerSpawned -= OnPlayerSpawned;
            }
        }
    }
}