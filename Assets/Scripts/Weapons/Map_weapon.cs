using UnityEngine;

/// <summary>
/// Map weapon (WeaponID = 3).
/// Shows the map with camera positions, objectives, and player location.
/// No ammo, just a UI display.
/// </summary>
public class Map_weapon : Weapon
{
    [Header("Components")]
    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject _uiSystem;
    [SerializeField] private GameObject _mapDisplay;

    protected override void Start()
    {
        base.Start();
        WeaponID = 3;
    }

    private void OnEnable()
    {
        if (_uiSystem != null) _uiSystem.SetActive(true);
        if (_mapDisplay != null) _mapDisplay.SetActive(true);
    }

    private void OnDisable()
    {
        if (_uiSystem != null) _uiSystem.SetActive(false);
        if (_mapDisplay != null) _mapDisplay.SetActive(false);
    }

    private void Update()
    {
        // Map is passive - just displays while equipped
        // Could add zoom with right click, etc.
    }
}