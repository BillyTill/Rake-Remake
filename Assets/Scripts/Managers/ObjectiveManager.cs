using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages game objectives (setup cameras, find Rake, kill Rake, etc).
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [SerializeField] private List<Objective> _objectives = new List<Objective>();
    private Objective _currentObjective;
    private int _currentIndex = 0;
    private bool _advancing;

    public Objective CurrentObjective => _currentObjective;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (_objectives.Count > 0) _currentObjective = _objectives[0];
        ShowCurrentObjective();
    }

    public void CompleteObjective(Objective objective)
    {
        if (_advancing) return;
        if (_currentObjective == objective)
        {
            objective.IsComplete = true;
            Debug.Log($"Objective completed: {objective.Name}");

            InformationUI.Instance?.DrawObjective($"Objective: {_currentObjective.Name} completed!");

            _advancing = true;
            StartCoroutine(NextObjectiveRoutine());
        }
    }

    private IEnumerator NextObjectiveRoutine()
    {
        yield return new WaitForSeconds(3f);
        SetupNextObjective();
        _advancing = false;
    }

    private void SetupNextObjective()
    {
        _currentIndex++;
        if (_currentIndex < _objectives.Count)
        {
            _currentObjective = _objectives[_currentIndex];
            ShowCurrentObjective();
        }
        else
        {
            _currentObjective = null;
            InformationUI.Instance?.DrawObjective("All objectives completed!");
        }
    }

    public void SetObjective(int index)
    {
        if (index >= 0 && index < _objectives.Count)
        {
            _currentIndex = index;
            _currentObjective = _objectives[index];
            ShowCurrentObjective();
        }
    }

    private void ShowCurrentObjective()
    {
        if (_currentObjective != null)
        {
            InformationUI.Instance?.DrawObjective($"Objective: {_currentObjective.Name}");
        }
    }
}

/// <summary>
/// Individual objective definition.
/// </summary>
[System.Serializable]
public class Objective
{
    public string Name;
    public string Description;
    public bool IsComplete { get; set; } = false;
}