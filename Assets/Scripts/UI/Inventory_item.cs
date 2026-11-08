using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component for inventory item slots.
/// Displays item name, type, and count.
/// </summary>
public class Inventory_item : MonoBehaviour
{
    [Header("Settings")]
    public string ItemName;
    public string ItemType;
    public int WeaponID;
    public int Count;

    [Header("UI")]
    [SerializeField] private Text _countText;

    private void Update()
    {
        if (_countText != null)
        {
            _countText.text = Count.ToString();
        }
    }
}