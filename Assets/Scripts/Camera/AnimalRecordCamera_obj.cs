using UnityEngine;

/// <summary>
/// Camera attached to an animal (boar) - "Pig Cam".
/// Created when player places camera on a trapped boar.
/// Provides mobile camera view following the animal.
/// </summary>
public class AnimalRecordCamera_obj : RECCameras_obj
{
    [Header("Components")]
    [SerializeField] private GameObject _setupCamera;     // Camera object to activate
    [SerializeField] private Transform _parent;           // Animal transform to follow
    [SerializeField] private Transform _collar;           // Collar visual
    [SerializeField] private Transform _backlight;        // Blinking light

    private void Update()
    {
        UpdateBacklightColor();
    }

    /// <summary>
    /// Called when player interacts with this camera.
    /// </summary>
    public override void Action()
    {
        if (WeaponManager.Instance != null && WeaponManager.Instance.CurrentWeaponID != 2)
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

    /// <summary>
    /// Attach camera to animal (called when player places camera on trapped boar).
    /// </summary>
    public override void SetupCamera()
    {
        if (_backlight != null) _backlight.gameObject.SetActive(false);
        if (_setupCamera != null) _setupCamera.SetActive(true);
        if (_collar != null) _collar.gameObject.SetActive(true);

        // Register with CameraManager
        if (CameraManager.Instance != null && _setupCamera != null)
        {
            CameraManager.Instance.AddCamera(_setupCamera.transform);
        }

        // Destroy this component (camera is now managed by Animal_AI)
        Destroy(this);
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