using UnityEngine;

/// <summary>
/// Objective: Setup cameras tutorial.
/// </summary>
public class SetupCameras_objective : MonoBehaviour
{
    [SerializeField] private int _requiredCameras = 3;
    [SerializeField] private ObjectiveManager _objectiveManager;

    private int _camerasSetup = 0;

    private void Start()
    {
        if (_objectiveManager == null) _objectiveManager = ObjectiveManager.Instance;
    }

    public void OnCameraSetup()
    {
        _camerasSetup++;
        if (_camerasSetup >= _requiredCameras)
        {
            CompleteObjective();
        }
    }

    private void CompleteObjective()
    {
        if (_objectiveManager != null && _objectiveManager.CurrentObjective != null)
        {
            _objectiveManager.CompleteObjective(_objectiveManager.CurrentObjective);
        }
    }
}