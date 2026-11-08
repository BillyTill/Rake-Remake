using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bear Trap weapon (WeaponID = 4).
/// Places a trap on the ground that damages and immobilizes the Rake for ~7 seconds.
/// Limited ammo, strategic placement required.
/// </summary>
public class Trap_weapon : Weapon
{
    [Header("Components")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private GameObject _trapPrefab;
    [SerializeField] private Animator _uiAnimator;
    [SerializeField] private GameObject _uiSystem;
    [SerializeField] private Text _ammoDisplay;

    private bool _isPlacing = false;

    protected override void Start()
    {
        base.Start();
        WeaponID = 4;
    }

    private void OnEnable()
    {
        if (_uiSystem != null) _uiSystem.SetActive(true);
        _isPlacing = false;
    }

    private void OnDisable()
    {
        if (_uiSystem != null) _uiSystem.SetActive(false);
    }

    private void Update()
    {
        // Update ammo display
        if (_ammoDisplay != null)
        {
            _ammoDisplay.text = WeaponManager.Instance.GetAmmo(WeaponID).ToString();
        }

        // Place trap
        if (Input.GetButtonDown("Fire1") && !_isPlacing)
        {
            if (WeaponManager.Instance.GetAmmo(WeaponID) > 0)
            {
                StartCoroutine(PlaceTrapRoutine());
            }
            else
            {
                InformationUI.Instance.DrawHint("No traps remaining");
            }
        }
    }

    private IEnumerator PlaceTrapRoutine()
    {
        _isPlacing = true;

        // Consume ammo
        WeaponManager.Instance.ConsumeAmmo(WeaponID, 1);

        // Animation
        if (_animator != null) _animator.SetBool("Setup", true);

        yield return new WaitForSeconds(0.7f);

        // Spawn trap
        if (_trapPrefab != null && _spawnPoint != null)
        {
            GameObject trap = Instantiate(_trapPrefab, _spawnPoint.position, _spawnPoint.rotation);
            LevelItemsManager.Instance.AddTrap(trap);
        }

        yield return new WaitForSeconds(0.2f);
        yield return new WaitForSeconds(0.5f);

        // Check if we have more traps
        if (WeaponManager.Instance.GetAmmo(WeaponID) > 0)
        {
            if (_animator != null) _animator.SetBool("Setup", false);
        }
        else
        {
            // Auto-remove weapon when out of ammo
            WeaponManager.Instance.RemoveWeapon(WeaponID);
            if (_uiSystem != null) _uiSystem.SetActive(false);
            gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(1f);
        _isPlacing = false;
    }
}