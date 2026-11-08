using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Medkit weapon (WeaponID = 8).
/// Heals player for 100 HP (max 100).
/// Consumable item.
/// </summary>
public class MedKit_weapon : Weapon
{
    [Header("Components")]
    [SerializeField] private Animator _animator;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _useSound;
    [SerializeField] private Animator _uiAnimator;
    [SerializeField] private GameObject _uiSystem;
    [SerializeField] private Text _ammoDisplay;

    private bool _isUsing = false;

    protected override void Start()
    {
        base.Start();
        WeaponID = 8;
    }

    private void OnEnable()
    {
        if (_uiSystem != null) _uiSystem.SetActive(true);
        _isUsing = false;
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

        // Use medkit
        if (Input.GetButtonDown("Fire1") && WeaponManager.Instance.GetAmmo(WeaponID) > 0 && !_isUsing)
        {
            StartCoroutine(UseRoutine());
        }
    }

    private IEnumerator UseRoutine()
    {
        _isUsing = true;

        // Consume ammo
        WeaponManager.Instance.ConsumeAmmo(WeaponID, 1);

        // Animation
        if (_animator != null) _animator.SetBool("Use", true);
        if (_audioSource != null && _useSound != null) _audioSource.PlayOneShot(_useSound);

        yield return new WaitForSeconds(0.1f);

        // Heal player
        if (FPSController.Instance != null)
        {
            FPSController.Instance.Health += 100;
            if (FPSController.Instance.Health > 100)
            {
                FPSController.Instance.Health = 100;
            }
        }

        yield return new WaitForSeconds(0.2f);
        _isUsing = false;

        yield return new WaitForSeconds(0.5f);

        // Check if we have more medkits
        if (WeaponManager.Instance.GetAmmo(WeaponID) > 0)
        {
            if (_animator != null) _animator.SetBool("Use", false);
        }
        else
        {
            // Auto-remove weapon when out of ammo
            WeaponManager.Instance.RemoveWeapon(WeaponID);
            if (_uiSystem != null) _uiSystem.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}