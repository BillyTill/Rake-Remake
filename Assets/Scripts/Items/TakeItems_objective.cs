using UnityEngine;

/// <summary>
/// Objective: Collect specific items.
/// </summary>
public class TakeItems_objective : MonoBehaviour
{
    [SerializeField] private ObjectiveManager _objectiveManager;
    [SerializeField] private int[] _requiredItemIDs; // Weapon IDs needed
    [SerializeField] private int[] _requiredCounts;

    private void Start()
    {
        if (_objectiveManager == null) _objectiveManager = ObjectiveManager.Instance;
    }

    private void Update()
    {
        if (_objectiveManager == null || _objectiveManager.CurrentObjective == null) return;

        CheckItems();
    }

    private void CheckItems()
    {
        if (WeaponManager.Instance == null) return;

        bool allCollected = true;
        for (int i = 0; i < _requiredItemIDs.Length; i++)
        {
            int itemID = _requiredItemIDs[i];
            int required = i < _requiredCounts.Length ? _requiredCounts[i] : 1;
            int have = WeaponManager.Instance.GetAmmo(itemID);

            if (have < required)
            {
                allCollected = false;
                break;
            }
        }

        if (allCollected)
        {
            _objectiveManager.CompleteObjective(_objectiveManager.CurrentObjective);
            enabled = false; // Disable after completion
        }
    }
}