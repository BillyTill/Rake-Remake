using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Camera weapon (WeaponID = 2).
/// Used to place stationary cameras on designated camera mounts (RECCameras_obj).
/// Max 5 cameras can be placed simultaneously.
/// </summary>
public class Camera_weapon : Weapon
{
    [Header("Components")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Animator _uiAnimator;
    [SerializeField] private GameObject _uiSystem;
    [SerializeField] private Text _ammoDisplay;

    private bool _isPlacing = false;

    protected override void Start()
    {
        base.Start();
        WeaponID = 2;
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

        // Raycast to find camera mount
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 2.5f))
        {
            var cameraMount = hit.collider.GetComponent<RECCameras_obj>();
            if (cameraMount != null)
            {
                // Show interaction prompt (handled by RECCameras_obj)
                if (Input.GetButtonDown("Use") && WeaponManager.Instance.GetAmmo(WeaponID) > 0 && !_isPlacing)
                {
                    if (cameraMount.CheckSetup())
                    {
                        StartCoroutine(PlaceCameraRoutine(cameraMount));
                    }
                }
            }
        }
    }

    private IEnumerator PlaceCameraRoutine(RECCameras_obj cameraMount)
    {
        _isPlacing = true;

        // Consume ammo
        WeaponManager.Instance.ConsumeAmmo(WeaponID, 1);

        // Animation
        if (_animator != null) _animator.SetBool("Setup", true);

        yield return new WaitForSeconds(0.1f);

        // Setup camera
        cameraMount.SetupCamera();

        yield return new WaitForSeconds(0.2f);
        _isPlacing = false;

        yield return new WaitForSeconds(0.5f);

        // Check if we have more cameras
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
    }
}