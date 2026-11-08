using UnityEngine;

/// <summary>
/// Binoculars weapon (WeaponID = 7).
/// Zoom view for long-range observation.
/// No ammo, toggle with left click.
/// </summary>
public class Binocular_weapon : Weapon
{
    [Header("Components")]
    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject _uiSystem;
    [SerializeField] private Camera _binocularsCamera;

    protected override void Start()
    {
        base.Start();
        WeaponID = 7;
    }

    private void OnEnable()
    {
        if (_uiSystem != null) _uiSystem.SetActive(true);
        if (_animator != null) _animator.SetBool("Sight", false);
        if (_binocularsCamera != null) _binocularsCamera.enabled = false;
    }

    private void OnDisable()
    {
        if (_uiSystem != null) _uiSystem.SetActive(false);
        if (_animator != null) _animator.SetBool("Sight", false);
        if (_binocularsCamera != null) _binocularsCamera.enabled = false;
    }

    private void Update()
    {
        // Toggle binoculars view with left mouse button
        if (Input.GetMouseButton(0))
        {
            if (_animator != null) _animator.SetBool("Sight", true);
            if (_binocularsCamera != null) _binocularsCamera.enabled = true;
        }
        else
        {
            if (_animator != null) _animator.SetBool("Sight", false);
            if (_binocularsCamera != null) _binocularsCamera.enabled = false;
        }
    }
}