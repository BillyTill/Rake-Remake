using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sniper Rifle weapon (WeaponID = 6).
/// Damage: 200 per shot, 2 shots before reload.
/// Long range precision weapon for dealing major damage to Rake.
/// Features scope with dedicated render camera.
/// </summary>
public class SniperRifle_weapon : Weapon
{
    [Header("Components")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Animator _uiAnimator;
    [SerializeField] private GameObject _uiSystem;
    [SerializeField] private Transform _caster;
    [SerializeField] private AudioSource _sfx;
    [SerializeField] private AudioClip _fireSound;
    [SerializeField] private AudioClip _reloadSound;
    [SerializeField] private Camera _scopeCamera; // Scope render camera
    [SerializeField] private GameObject _muzzlePos;
    [SerializeField] private GameObject _muzzleFlash;
    [SerializeField] private GameObject[] _decals; // [0] = default, [1] = flesh
    [SerializeField] private CameraMouseLook _camMouseLook;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private GameObject _scopeOverlay;
    [SerializeField] private Material _defaultMat;
    [SerializeField] private Material _scopeMat;

    [Header("Stats")]
    [SerializeField] private int _clipSize = 2;
    private int _currentAmmo = 2;
    private bool _isSighting = false;
    private bool _isReloading = false;
    private bool _canFire = true;

    private GameObject _ammoTextObj;

    protected override void Start()
    {
        base.Start();
        WeaponID = 6;
        _ammoTextObj = _uiSystem.transform.Find("Ammo_display")?.gameObject;
    }

    private void OnEnable()
    {
        if (_uiSystem != null) _uiSystem.SetActive(true);
        _isSighting = false;
        _isReloading = false;
        _canFire = true;
        _currentAmmo = Mathf.Min(_clipSize, WeaponManager.Instance.GetAmmo(WeaponID));
        if (_uiAnimator != null) _uiAnimator.SetInteger("Bullets", _currentAmmo);

        // Reset camera sensitivity
        if (_camMouseLook != null)
        {
            _camMouseLook.SensitivityX = 2f;
            _camMouseLook.SensitivityY = 2f;
        }
        if (_scopeCamera != null) _scopeCamera.enabled = false;
        if (_animator != null) _animator.SetBool("Sight", false);
        if (_playerMovement != null) _playerMovement.CanMove = true;
    }

    private void OnDisable()
    {
        // Restore player movement and camera
        if (_playerMovement != null) _playerMovement.CanMove = true;
        if (_camMouseLook != null)
        {
            _camMouseLook.SensitivityX = 2f;
            _camMouseLook.SensitivityY = 2f;
        }
        if (_scopeCamera != null) _scopeCamera.enabled = false;
        if (_animator != null) _animator.SetBool("Sight", false);
        if (_uiSystem != null) _uiSystem.SetActive(false);
    }

    private void Update()
    {
        // Update ammo display
        int reserveAmmo = WeaponManager.Instance.GetAmmo(WeaponID);
        if (_ammoTextObj != null)
        {
            _ammoTextObj.GetComponent<Text>().text = reserveAmmo.ToString();
        }

        // Scope material toggle (visual feedback on right click press/release)
        if (Input.GetMouseButtonDown(1) && _scopeOverlay != null)
        {
            _scopeOverlay.GetComponent<Renderer>().material = _scopeMat;
        }
        if (Input.GetMouseButtonUp(1) && _scopeOverlay != null)
        {
            _scopeOverlay.GetComponent<Renderer>().material = _defaultMat;
        }

        // Sighting (right click hold)
        if (Input.GetButton("Fire2"))
        {
            _isSighting = true;
        }
        else
        {
            _isSighting = false;
        }

        // Fire
        if (Input.GetButtonDown("Fire1") && _isSighting && _canFire && _currentAmmo > 0)
        {
            StartCoroutine(FireRoutine());
        }

        // Reload
        if (Input.GetButtonDown("Reload") && _currentAmmo < _clipSize && reserveAmmo > 0 && !_isReloading)
        {
            StartCoroutine(ReloadRoutine());
        }

        // Scope state
        if (_isSighting)
        {
            if (_playerMovement != null) _playerMovement.CanMove = false;
            if (_camMouseLook != null)
            {
                _camMouseLook.SensitivityX = 0.5f;
                _camMouseLook.SensitivityY = 0.5f;
            }
            if (_scopeCamera != null) _scopeCamera.enabled = true;
            if (_animator != null) _animator.SetBool("Sight", true);
        }
        else
        {
            if (_playerMovement != null) _playerMovement.CanMove = true;
            if (_camMouseLook != null)
            {
                _camMouseLook.SensitivityX = 2f;
                _camMouseLook.SensitivityY = 2f;
            }
            if (_scopeCamera != null) _scopeCamera.enabled = false;
            if (_animator != null) _animator.SetBool("Sight", false);
        }
    }

    private IEnumerator FireRoutine()
    {
        _canFire = false;

        // Muzzle flash
        if (_muzzleFlash != null && _muzzlePos != null)
        {
            Instantiate(_muzzleFlash, _muzzlePos.transform.position, _muzzlePos.transform.rotation);
        }

        // Animation
        if (_animator != null) _animator.SetBool("Fire", true);

        // Ammo
        _currentAmmo--;
        if (_uiAnimator != null) _uiAnimator.SetInteger("Bullets", _currentAmmo);

        // Sound
        if (_sfx != null && _fireSound != null) _sfx.PlayOneShot(_fireSound);

        // Raycast
        Ray ray = new Ray(_caster.position, _caster.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            // Notify AI of player fire
            NotificationsManager.Instance.PostNotification(this, "Event_PlayerFire");

            // Damage unit (200 damage - high damage for sniper)
            var unit = hit.collider.GetComponent<Unit_obj>();
            if (unit != null)
            {
                unit.TakeDamage(200, "SniperShot");
                if (_decals.Length > 1)
                {
                    Instantiate(_decals[1], hit.point + hit.normal * 0.001f, Quaternion.identity);
                }
            }

            // Damage window (100 damage)
            var window = hit.collider.GetComponent<Window_obj>();
            if (window != null)
            {
                window.TakeDamage(100, "SniperShot");
            }

            // Default decal
            if (_decals.Length > 0)
            {
                Instantiate(_decals[0], hit.point + hit.normal * 0.001f, Quaternion.identity);
            }
        }

        yield return new WaitForSeconds(0.2f);

        if (_animator != null) _animator.SetBool("Fire", false);

        yield return new WaitForSeconds(1f); // Fire rate cooldown

        _canFire = true;
    }

    private IEnumerator ReloadRoutine()
    {
        _canFire = false;
        _isReloading = true;

        if (_animator != null) _animator.SetBool("Reload", true);

        yield return new WaitForSeconds(0.5f);

        if (_sfx != null && _reloadSound != null) _sfx.PlayOneShot(_reloadSound);

        yield return new WaitForSeconds(1f);

        // Calculate ammo to add
        int needed = _clipSize - _currentAmmo;
        int available = WeaponManager.Instance.GetAmmo(WeaponID);
        int addAmount = Mathf.Min(needed, available);

        WeaponManager.Instance.ConsumeAmmo(WeaponID, addAmount);
        _currentAmmo += addAmount;

        if (_uiAnimator != null) _uiAnimator.SetInteger("Bullets", _currentAmmo);

        _canFire = true;
        _isReloading = false;

        if (_animator != null) _animator.SetBool("Reload", false);
    }
}