using System.Collections;
using UnityEngine;

/// <summary>
/// Mission timer - 3 hours real time limit.
/// Game divided into 3 nights, must kill Rake by night 3.
/// </summary>
public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float _maxMissionTime = 10800f; // 3 hours in seconds
    [SerializeField] private float _dayDuration = 1800f;      // 30 minutes per day
    [SerializeField] private float _nightDuration = 1800f;    // 30 minutes per night

    private float _elapsedTime = 0f;
    private int _currentNight = 0; // 0=day prep, 1=night 1, 2=night 2, 3=night 3
    private bool _isRunning = true;
    private bool _isDayPhase = true;

    public float ElapsedTime => _elapsedTime;
    public float RemainingTime => Mathf.Max(0, _maxMissionTime - _elapsedTime);
    public int CurrentNight => _currentNight;
    public bool IsDayPhase => _isDayPhase;
    public bool IsNightPhase => !_isDayPhase;
    public bool IsGameOver => _elapsedTime >= _maxMissionTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!_isRunning) return;

        _elapsedTime += Time.deltaTime;

        // Check phase transitions
        UpdatePhase();

        // Check game over
        if (IsGameOver)
        {
            GameOver();
        }
    }

    private void UpdatePhase()
    {
        float cycleTime = _dayDuration + _nightDuration;
        float cycleProgress = _elapsedTime % cycleTime;

        bool wasDay = _isDayPhase;
        _isDayPhase = cycleProgress < _dayDuration;

        // Night counter
        int newNight = Mathf.FloorToInt(_elapsedTime / cycleTime) + 1;
        if (newNight != _currentNight && !_isDayPhase)
        {
            _currentNight = newNight;
            OnNightStart(_currentNight);
        }

        if (wasDay != _isDayPhase)
        {
            if (_isDayPhase)
            {
                OnDayStart();
            }
            else
            {
                OnNightStart(_currentNight);
            }
        }
    }

    private void OnDayStart()
    {
        Debug.Log($"[GameTimer] Day {_currentNight} started - Preparation phase");
        InformationUI.Instance?.DrawObjective($"Day {_currentNight}: Prepare for the hunt");
        // Rake hides in cave, player can place cameras, gather supplies
    }

    private void OnNightStart(int night)
    {
        Debug.Log($"[GameTimer] Night {night} started - Hunt phase");
        InformationUI.Instance?.DrawObjective($"Night {night}: Hunt the Rake");

        // Rake behavior changes by night
        var rake = GameObject.FindGameObjectWithTag("Rake");
        if (rake != null)
        {
            var ai = rake.GetComponent<Enemy_AI>();
            if (ai != null)
            {
                // Night 1: Passive, Night 2: Active, Night 3: Very aggressive
                // This would be handled by Enemy_AI internally based on night number
            }
        }
    }

    private void GameOver()
    {
        _isRunning = false;
        InformationUI.Instance?.DrawObjective("Time's up! The Rake escaped...");
        FPSController.Instance?.KillPlayer();
    }

    /// <summary>
    /// Skip day phase (player chooses to skip preparation).
    /// </summary>
    public void SkipDay()
    {
        if (_isDayPhase)
        {
            _elapsedTime += _dayDuration - (_elapsedTime % (_dayDuration + _nightDuration));
        }
    }

    /// <summary>
    /// Get formatted time string.
    /// </summary>
    public string GetTimeString()
    {
        float remaining = RemainingTime;
        int hours = Mathf.FloorToInt(remaining / 3600f);
        int minutes = Mathf.FloorToInt((remaining % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// Get phase time remaining.
    /// </summary>
    public string GetPhaseTimeString()
    {
        float cycleTime = _dayDuration + _nightDuration;
        float cycleProgress = _elapsedTime % cycleTime;
        float phaseRemaining = _isDayPhase ? _dayDuration - cycleProgress : _nightDuration - (cycleProgress - _dayDuration);
        
        int minutes = Mathf.FloorToInt(phaseRemaining / 60f);
        int seconds = Mathf.FloorToInt(phaseRemaining % 60f);
        return $"{(_isDayPhase ? "Day" : "Night")} {_currentNight}: {minutes:00}:{seconds:00}";
    }
}