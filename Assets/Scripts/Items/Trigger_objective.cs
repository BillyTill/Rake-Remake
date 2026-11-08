using UnityEngine;

/// <summary>
/// Trigger that completes an objective when entered.
/// </summary>
public class Trigger_objective : MonoBehaviour
{
    [SerializeField] private ObjectiveManager _objectiveManager;
    [SerializeField] private bool _triggerOnce = true;

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered && _triggerOnce) return;

        if (other.CompareTag("Player"))
        {
            _triggered = true;
            if (_objectiveManager != null && _objectiveManager.CurrentObjective != null)
            {
                _objectiveManager.CompleteObjective(_objectiveManager.CurrentObjective);
            }
        }
    }
}