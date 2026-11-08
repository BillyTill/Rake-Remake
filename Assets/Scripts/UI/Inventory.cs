using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player inventory UI system.
/// Displays owned weapons/items and allows selection.
/// </summary>
public class Inventory : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private List<GameObject> _itemSlots = new List<GameObject>(); // UI slots for each weapon ID
    [SerializeField] private RectTransform _cursorPosition; // Follows mouse
    [SerializeField] private GameObject _drawer;            // Background panel
    [SerializeField] private GameObject _inventoryPanel;    // Main inventory panel
    [SerializeField] private Text _tooltipText;             // Item info tooltip

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _openSound;
    [SerializeField] private AudioClip _closeSound;

    public bool IsOpen { get; private set; } = false;

    private void Update()
    {
        if (!IsOpen) return;

        // Follow mouse cursor
        if (_cursorPosition != null)
        {
            _cursorPosition.anchoredPosition = new Vector2(
                Input.mousePosition.x,
                Input.mousePosition.y - _cursorPosition.sizeDelta.y
            );
        }

        // Tooltip on hover
        if (_tooltipText != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var item = hit.collider.GetComponent<Inventory_item>();
                if (item != null)
                {
                    _drawer.SetActive(true);
                    _tooltipText.text = $"Name: {item.ItemName}\nType: {item.ItemType}\nCount: {item.Count}";
                    
                    // Select on click
                    if (Input.GetButtonDown("Fire1"))
                    {
                        if (WeaponManager.Instance != null)
                        {
                            WeaponManager.Instance.SelectWeaponByID(item.WeaponID);
                            FPSController.Instance.OnWeaponSelected();
                        }
                    }
                }
                else
                {
                    _drawer.SetActive(false);
                }
            }
            else
            {
                _drawer.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Open inventory UI.
    /// </summary>
    public void OpenInventory()
    {
        _audioSource?.PlayOneShot(_openSound);
        _drawer?.SetActive(true);
        _inventoryPanel?.SetActive(true);
        IsOpen = true;
        RefreshItems();
    }

    /// <summary>
    /// Close inventory UI.
    /// </summary>
    public void CloseInventory()
    {
        _audioSource?.PlayOneShot(_closeSound);
        _inventoryPanel?.SetActive(false);
        _drawer?.SetActive(false);
        IsOpen = false;
    }

    /// <summary>
    /// Refresh displayed items from WeaponManager.
    /// </summary>
    private void RefreshItems()
    {
        if (WeaponManager.Instance == null) return;

        // Hide all slots
        foreach (var slot in _itemSlots)
        {
            slot.SetActive(false);
        }

        // Show owned weapons
        var weapons = WeaponManager.Instance.PlayerWeapons;
        for (int i = 0; i < weapons.Count; i++)
        {
            var weapon = weapons[i].GetComponent<Weapon>();
            if (weapon != null && weapon.WeaponID < _itemSlots.Count)
            {
                var slot = _itemSlots[weapon.WeaponID];
                slot.SetActive(true);
                var itemUI = slot.GetComponent<Inventory_item>();
                if (itemUI != null)
                {
                    itemUI.Count = weapon.Ammo;
                }
            }
        }
    }
}