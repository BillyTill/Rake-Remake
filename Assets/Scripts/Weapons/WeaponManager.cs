using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages player weapon inventory, switching, and ammo.
/// Singleton for global access.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    [Header("Weapon References")]
    [SerializeField] private List<Transform> _allWeapons = new List<Transform>(); // All available weapon prefabs (indexed by WeaponID)
    [SerializeField] private Animator _animator; // Weapon switch animator

    [Header("Runtime State")]
    private List<Transform> _playerWeapons = new List<Transform>(); // Currently owned weapons
    private List<int> _weaponsAmmo = new List<int>(); // Ammo per weapon ID (indexed by WeaponID)
    private int _currentWeaponIndex = 0;
    private Transform _currentWeaponTransform;
    private bool _isChangingWeapon = false;

    // Properties
    public int CurrentWeaponID => _currentWeaponTransform != null ? _currentWeaponTransform.GetComponent<Weapon>().WeaponID : 0;
    public int CurrentWeaponIndex => _currentWeaponIndex;
    public int PlayerWeaponsCount => _playerWeapons.Count;
    public int AllWeaponsCount => _allWeapons.Count;
    public int WeaponsAmmoCount => _weaponsAmmo.Count;
    public bool IsChangingWeapon => _isChangingWeapon;
    public IReadOnlyList<Transform> PlayerWeapons => _playerWeapons;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize ammo array for all possible weapon IDs (1-8)
        for (int i = 0; i <= 8; i++)
        {
            _weaponsAmmo.Add(0);
        }
    }

    private void Update()
    {
        if (_isChangingWeapon) return;
        if (FPSController.Instance == null || FPSController.Instance.State != FPSController.PlayerState.Full) return;

        // Weapon selection keys (1-8 mapped to specific weapon IDs)
        HandleWeaponInput();
    }

    private void HandleWeaponInput()
    {
        // Key 1: Rifle (Shotgun) - ID 1
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TrySelectWeaponByID(1);
        }
        // Key 2: Camera - ID 2
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TrySelectWeaponByID(2);
        }
        // Key 3: Map - ID 3
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TrySelectWeaponByID(3);
        }
        // Key 4: Bear Trap - ID 4
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TrySelectWeaponByID(4);
        }
        // Key 5: Food/Bait - ID 5
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            TrySelectWeaponByID(5);
        }
        // Key 6: Sniper Rifle - ID 6
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            TrySelectWeaponByID(6);
        }
        // Key 7: Binoculars - ID 7
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            TrySelectWeaponByID(7);
        }
        // Key 8: Medkit - ID 8
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            TrySelectWeaponByID(8);
        }
    }

    private void TrySelectWeaponByID(int weaponID)
    {
        if (HasWeapon(weaponID))
        {
            SelectWeaponByID(weaponID);
        }
        else
        {
            string weaponName = GetWeaponName(weaponID);
            InformationUI.Instance.DrawHint($"You do not have {weaponName}");
        }
    }

    private string GetWeaponName(int weaponID)
    {
        switch (weaponID)
        {
            case 1: return "rifle";
            case 2: return "camera";
            case 3: return "map";
            case 4: return "trap";
            case 5: return "bait";
            case 6: return "sniper rifle";
            case 7: return "binoculars";
            case 8: return "medkit";
            default: return "item";
        }
    }

    /// <summary>
    /// Check if player owns a weapon.
    /// </summary>
    public bool HasWeapon(int weaponID)
    {
        foreach (var weapon in _playerWeapons)
        {
            var w = weapon.GetComponent<Weapon>();
            if (w != null && w.WeaponID == weaponID)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Check if ammo is full for a weapon.
    /// </summary>
    public bool IsAmmoFull(int weaponID)
    {
        if (weaponID >= 0 && weaponID < _weaponsAmmo.Count)
        {
            return _weaponsAmmo[weaponID] >= 1000; // Max ammo cap
        }
        return false;
    }

    /// <summary>
    /// Get current ammo for a weapon.
    /// </summary>
    public int GetAmmo(int weaponID)
    {
        if (weaponID >= 0 && weaponID < _weaponsAmmo.Count)
        {
            return _weaponsAmmo[weaponID];
        }
        return 0;
    }

    /// <summary>
    /// Set ammo for a weapon.
    /// </summary>
    public void SetAmmo(int weaponID, int amount)
    {
        if (weaponID >= 0 && weaponID < _weaponsAmmo.Count)
        {
            _weaponsAmmo[weaponID] = Mathf.Max(0, amount);
        }
    }

    /// <summary>
    /// Add weapon to player inventory.
    /// </summary>
    /// <param name="weaponID">Weapon ID to add</param>
    /// <param name="ammoCount">Initial ammo count</param>
    /// <param name="showHint">Whether to show pickup hint</param>
    public void AddWeapon(int weaponID, int ammoCount, bool showHint = true)
    {
        if (HasWeapon(weaponID))
        {
            AddAmmo(weaponID, ammoCount);
            if (showHint)
            {
                InformationUI.Instance.PickupInfo($"+ {GetWeaponName(weaponID)} ammo: {ammoCount}");
            }
            return;
        }

        if (weaponID >= 0 && weaponID < _allWeapons.Count)
        {
            var weaponTransform = _allWeapons[weaponID];
            if (weaponTransform != null)
            {
                _playerWeapons.Add(weaponTransform);
                AddAmmo(weaponID, ammoCount);

                // Activate the weapon GameObject (view model)
                weaponTransform.gameObject.SetActive(false); // Initially hidden until selected

                if (showHint)
                {
                    InformationUI.Instance.PickupInfo($"+ {GetWeaponName(weaponID)}");
                }

                // Auto-select if first weapon
                if (_playerWeapons.Count == 1)
                {
                    SelectWeapon(0);
                }
            }
        }
    }

    /// <summary>
    /// Remove weapon from player inventory.
    /// </summary>
    public void RemoveWeapon(int weaponID)
    {
        for (int i = 0; i < _playerWeapons.Count; i++)
        {
            var weapon = _playerWeapons[i].GetComponent<Weapon>();
            if (weapon != null && weapon.WeaponID == weaponID)
            {
                if (weaponID == CurrentWeaponID)
                {
                    // Switch to first available weapon
                    _currentWeaponID = 0;
                    if (_playerWeapons.Count > 1)
                    {
                        _currentWeaponTransform = _playerWeapons[0];
                    }
                    else
                    {
                        _currentWeaponTransform = null;
                    }
                }

                _playerWeapons[i].gameObject.SetActive(false);
                _playerWeapons.RemoveAt(i);
                break;
            }
        }
    }

    /// <summary>
    /// Add ammo to a weapon.
    /// </summary>
    public void AddAmmo(int weaponID, int count)
    {
        if (weaponID >= 0 && weaponID < _weaponsAmmo.Count)
        {
            _weaponsAmmo[weaponID] = Mathf.Min(1000, _weaponsAmmo[weaponID] + count);
        }
    }

    /// <summary>
    /// Consume ammo (returns true if successful).
    /// </summary>
    public bool ConsumeAmmo(int weaponID, int count = 1)
    {
        if (weaponID >= 0 && weaponID < _weaponsAmmo.Count && _weaponsAmmo[weaponID] >= count)
        {
            _weaponsAmmo[weaponID] -= count;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Select weapon by index in playerWeapons list.
    /// </summary>
    public void SelectWeapon(int index)
    {
        if (_playerWeapons.Count == 0) return;
        if (index < 0 || index >= _playerWeapons.Count) return;
        if (_isChangingWeapon) return;

        StartCoroutine(ChangeWeaponRoutine(index));
    }

    /// <summary>
    /// Select weapon by WeaponID.
    /// </summary>
    public void SelectWeaponByID(int weaponID)
    {
        for (int i = 0; i < _playerWeapons.Count; i++)
        {
            var weapon = _playerWeapons[i].GetComponent<Weapon>();
            if (weapon != null && weapon.WeaponID == weaponID)
            {
                SelectWeapon(i);
                return;
            }
        }
    }

    /// <summary>
    /// Hide current weapon (used when entering camera mode, inventory, etc).
    /// </summary>
    public void HideCurrentWeapon()
    {
        if (_currentWeaponTransform != null)
        {
            StartCoroutine(HideWeaponRoutine(_currentWeaponIndex));
        }
    }

    private IEnumerator ChangeWeaponRoutine(int newIndex)
    {
        _isChangingWeapon = true;

        // Play switch animation
        if (_animator != null)
        {
            _animator.SetBool("ChangeWeapon", true);
        }

        yield return new WaitForSeconds(0.5f);

        // Hide old weapon
        if (_currentWeaponTransform != null)
        {
            _currentWeaponTransform.gameObject.SetActive(false);
            var oldWeapon = _currentWeaponTransform.GetComponent<Weapon>();
            if (oldWeapon != null) oldWeapon.OnUnequip();
        }

        if (_animator != null)
        {
            _animator.SetBool("ChangeWeapon", false);
        }

        // Show new weapon
        _currentWeaponTransform = _playerWeapons[newIndex];
        _currentWeaponTransform.gameObject.SetActive(true);
        _currentWeaponIndex = newIndex;

        var newWeapon = _currentWeaponTransform.GetComponent<Weapon>();
        if (newWeapon != null) newWeapon.OnEquip();

        yield return new WaitForSeconds(1f);

        _isChangingWeapon = false;
    }

    private IEnumerator HideWeaponRoutine(int index)
    {
        _isChangingWeapon = true;

        if (_animator != null)
        {
            _animator.SetBool("ChangeWeapon", true);
        }

        yield return new WaitForSeconds(0.5f);

        if (_currentWeaponTransform != null)
        {
            _currentWeaponTransform.gameObject.SetActive(false);
            var weapon = _currentWeaponTransform.GetComponent<Weapon>();
            if (weapon != null) weapon.OnUnequip();
        }

        if (_animator != null)
        {
            _animator.SetBool("ChangeWeapon", false);
        }

        _currentWeaponID = 0;
        _currentWeaponTransform = null;

        // Select first available weapon
        if (_playerWeapons.Count > 0)
        {
            SelectWeapon(0);
        }

        yield return new WaitForSeconds(1f);
        _isChangingWeapon = false;
    }

    /// <summary>
    /// Get weapon transform at playerWeapons index.
    /// </summary>
    public Transform GetWeaponAtIndex(int index)
    {
        if (index >= 0 && index < _playerWeapons.Count)
            return _playerWeapons[index];
        return null;
    }

    /// <summary>
    /// Get allWeapons transform by index.
    /// </summary>
    public Transform GetAllWeaponAtIndex(int index)
    {
        if (index >= 0 && index < _allWeapons.Count)
            return _allWeapons[index];
        return null;
    }

    /// <summary>
    /// Clear all player weapons (used on load/new game).
    /// </summary>
    public void ClearPlayerWeapons()
    {
        foreach (var weapon in _playerWeapons)
        {
            if (weapon != null)
                weapon.gameObject.SetActive(false);
        }
        _playerWeapons.Clear();
        _currentWeaponTransform = null;
        _currentWeaponIndex = 0;
        _currentWeaponID = 0;
    }

    // Backward compatibility
    private int _currentWeaponID = 0;
    public int currentWeaponID => _currentWeaponID;
}