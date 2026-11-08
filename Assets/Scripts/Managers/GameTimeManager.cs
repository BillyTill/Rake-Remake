using UnityEngine;

/// <summary>
/// Manages in-game time (day/night cycle tracking).
/// Time scale: 1 real second = 1 game minute (configurable).
/// </summary>
public class GameTimeManager : MonoBehaviour
{
    public static GameTimeManager Instance { get; private set; }

    [Header("Date")]
    [SerializeField] private int _day = 1;
    [SerializeField] private int _month = 1;
    [SerializeField] private int _year = 2015;

    [Header("Time")]
    [SerializeField] private float _seconds = 0f;
    [SerializeField] private int _secondsInt = 0;
    [SerializeField] private float _minutes = 0f;
    [SerializeField] private float _hours = 6f; // Start at 6 AM

    [Header("Settings")]
    [SerializeField] private float _timeScale = 1f; // 1 real second = 1 game minute

    // Properties
    public int Day => _day;
    public int Month => _month;
    public int Year => _year;
    public float Hours => _hours;
    public float Minutes => _minutes;
    public int Seconds => _secondsInt;
    public bool IsNight => _hours >= 20f || _hours < 6f;
    public bool IsDay => !IsNight;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        _seconds += Time.deltaTime * _timeScale;
        _secondsInt = Mathf.FloorToInt(_seconds);

        if (_seconds >= 60f)
        {
            _seconds = 0f;
            _minutes += 1f;

            if (_minutes >= 60f)
            {
                _minutes = 0f;
                _hours += 1f;

                if (_hours >= 24f)
                {
                    _hours = 0f;
                    _day += 1;

                    if (_day > 30) // Simplified month handling
                    {
                        _day = 1;
                        _month += 1;
                        if (_month > 12)
                        {
                            _month = 1;
                            _year += 1;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get formatted time string (HH:MM).
    /// </summary>
    public string GetTimeString()
    {
        return string.Format("{0:00}:{1:00}", Mathf.FloorToInt(_hours), Mathf.FloorToInt(_minutes));
    }

    /// <summary>
    /// Get formatted date string.
    /// </summary>
    public string GetDateString()
    {
        return string.Format("{0:00}.{1:00}.{2}", _day, _month, _year);
    }

    /// <summary>
    /// Set time scale (for testing or time acceleration).
    /// </summary>
    public void SetTimeScale(float scale)
    {
        _timeScale = Mathf.Max(0f, scale);
    }

    /// <summary>
    /// Skip to next night (used when player chooses to skip day).
    /// </summary>
    public void SkipToNight()
    {
        _hours = 20f;
        _minutes = 0f;
        _seconds = 0f;
    }

    /// <summary>
    /// Skip to next day (used when night ends).
    /// </summary>
    public void SkipToDay()
    {
        _hours = 6f;
        _minutes = 0f;
        _seconds = 0f;
        _day += 1;
    }
}