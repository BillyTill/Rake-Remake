using UnityEngine;

/// <summary>
/// Time pause utility - pauses/unpauses game time.
/// </summary>
public class TimePause : MonoBehaviour
{
    private static bool _isPaused = false;
    private static float _previousTimeScale = 1f;

    public static bool IsPaused => _isPaused;

    public static void Pause()
    {
        if (!_isPaused)
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _isPaused = true;
        }
    }

    public static void Resume()
    {
        if (_isPaused)
        {
            Time.timeScale = _previousTimeScale;
            _isPaused = false;
        }
    }

    public static void Toggle()
    {
        if (_isPaused) Resume(); else Pause();
    }
}