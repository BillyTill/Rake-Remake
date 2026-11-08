using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stationary camera that can be placed on designated mounts.
/// Max 5 cameras can be active simultaneously.
/// </summary>
public class RecordCamera_obj : RECCameras_obj
{
    public enum CameraState
    {
        Unsetup,  // Mount is empty, camera can be placed
        Setup     // Camera is placed and active
    }

    [Header("Components")]
    [SerializeField] private Text _interactionText;
    [SerializeField] private GameObject _setupCamera;     // The actual camera object (child)
    [SerializeField] private GameObject _cameraPoint;     // Visual mount point when unsetup
    [SerializeField] private GameObject _cameraRender;    // Optional render preview
    [SerializeField] private Transform _backlight;        // Blinking light indicator
    [SerializeField] private AudioClip _setupSound;

    [Header("Settings")]
    [SerializeField] private int _cameraID = 0;           // Unique ID for save/load

    public CameraState CurrentState { get; private set; } = CameraState.Unsetup;
    public int CameraID => _cameraID;
    public bool IsRecording { get; set; } = false;

    private void Start()
    {
        _interactionText.text = "Press 'E' to setup camera";
        SetUnsetupState(true);
    }

    private void Update()
    {
        if (CurrentState == CameraState.Unsetup)
        {
            UpdateBacklightColor();
        }
    }

    /// <summary>
    /// Set camera to setup state (active).
    /// </summary>
    public void SetSetupState()
    {
        CurrentState = CameraState.Setup;

        if (_setupCamera != null) _setupCamera.SetActive(true);
        if (_cameraPoint != null) _cameraPoint.SetActive(false);
        if (_cameraRender != null) _cameraRender.SetActive(false);

        // Register with CameraManager
        if (CameraManager.Instance != null && _setupCamera != null)
        {
            CameraManager.Instance.AddCamera(_setupCamera.transform);
        }

        // Play sound
        if (_setupSound != null)
        {
            AudioSource.PlayClipAtPoint(_setupSound, transform.position);
        }

        _interactionText.text = "Press 'E' to pickup camera";
    }

    /// <summary>
    /// Set camera to unsetup state (empty mount).
    /// </summary>
    public void SetUnsetupState(bool silent = false)
    {
        CurrentState = CameraState.Unsetup;

        if (_setupCamera != null) _setupCamera.SetActive(false);
        if (_cameraPoint != null) _cameraPoint.SetActive(true);
        if (_cameraRender != null) _cameraRender.SetActive(false);

        // Unregister from CameraManager
        if (CameraManager.Instance != null && _setupCamera != null)
        {
            CameraManager.Instance.RemoveCamera(_setupCamera.transform);
        }

        // Play sound
        if (!silent && _setupSound != null)
        {
            AudioSource.PlayClipAtPoint(_setupSound, transform.position);
        }

        _interactionText.text = "Press 'E' to setup camera";
    }

    /// <summary>
    /// Called when player uses camera weapon on this mount.
    /// </summary>
    public override void SetupCamera()
    {
        if (CurrentState == CameraState.Unsetup)
        {
            SetSetupState();
        }
    }

    /// <summary>
    /// Check if camera can be placed here.
    /// </summary>
    public override bool CheckSetup()
    {
        if (CurrentState == CameraState.Unsetup)
        {
            // Check global camera limit (max 5)
            if (CameraManager.Instance != null && CameraManager.Instance.CameraCount >= 5)
            {
                InformationUI.Instance.DrawHint("You cannot setup more than 5 cameras");
                return false;
            }
            return true;
        }
        else
        {
            // Pickup camera
            PickupCamera();
            return false;
        }
    }

    /// <summary>
    /// Player picks up the camera (returns to inventory).
    /// </summary>
    private void PickupCamera()
    {
        if (WeaponManager.Instance != null)
        {
            if (WeaponManager.Instance.HasWeapon(2))
            {
                if (WeaponManager.Instance.IsAmmoFull(2))
                {
                    InformationUI.Instance.DrawHint("Full count of cameras");
                    return;
                }
                WeaponManager.Instance.AddAmmo(2, 1);
                InformationUI.Instance.PickupInfo("+ Add camera 1");
            }
            else
            {
                WeaponManager.Instance.AddWeapon(2, 1, true);
                InformationUI.Instance.PickupInfo("+ Add camera 1");
            }
        }
        SetUnsetupState();
    }

    /// <summary>
    /// Called when player interacts (E key) with this object.
    /// </summary>
    public override void Action()
    {
        if (CurrentState == CameraState.Unsetup && WeaponManager.Instance != null)
        {
            if (WeaponManager.Instance.CurrentWeaponID != 2)
            {
                if (WeaponManager.Instance.HasWeapon(2))
                {
                    InformationUI.Instance.DrawHint("Please select camera");
                }
                else
                {
                    InformationUI.Instance.DrawHint("You do not have camera");
                }
            }
        }
        else if (CurrentState == CameraState.Setup && WeaponManager.Instance != null)
        {
            if (WeaponManager.Instance.CurrentWeaponID != 2)
            {
                PickupCamera();
            }
        }
    }

    /// <summary>
    /// Update backlight blinking effect.
    /// </summary>
    private void UpdateBacklightColor()
    {
        if (_backlight != null)
        {
            var renderer = _backlight.GetComponent<Renderer>();
            if (renderer != null)
            {
                float alpha = Mathf.PingPong(Time.time * 0.1f, 0.1f);
                Color color = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, alpha);
                renderer.material.SetColor("_TintColor", color);
            }
        }
    }
}