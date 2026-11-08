using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shotgun/Rifle weapon (WeaponID = 1).
/// Damage: 25 per shot, 2 shots before reload, stuns Rake for 4 seconds.
/// Close range weapon for crowd control.
/// </summary>
public class Rifle_weapon : Weapon
{
    [Header("Components")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Animator _uiAnimator;
    [SerializeField] private GameObject _uiSystem;
    [SerializeField] private Transform _caster; // Raycast origin
    [SerializeField] private AudioSource _sfx;
    [SerializeField] private AudioClip _fireSound;
    [SerializeField] private AudioClip _reloadSound;
    [SerializeField] private GameObject _muzzlePos;
    [SerializeField] private GameObject _muzzleFlash;
    [SerializeField] private GameObject[] _decals; // [0] = default, [1] = flesh

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
        WeaponID = 1;
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
    }

    private void OnDisable()
    {
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

        // Animator sight state
        if (_animator != null)
        {
            _animator.SetBool("Sight", _isSighting);
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
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            // Notify AI of player fire
            NotificationsManager.Instance.PostNotification(this, "Event_PlayerFire");

            // Damage unit
            var unit = hit.collider.GetComponent<Unit_obj>();
            if (unit != null)
            {
                unit.TakeDamage(25, "SniperShot"); // Rifle does 25 damage
                if (_decals.Length > 1)
                {
                    Instantiate(_decals[1], hit.point + hit.normal * 0.001f, Quaternion.identity);
                }
            }

            // Damage window
            var window = hit.collider.GetComponent<Window_obj>();
            if (window != null)
            {
                window.TakeDamage(25, "SniperShot");
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

        yield return new WaitForSeconds(1.5f);

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