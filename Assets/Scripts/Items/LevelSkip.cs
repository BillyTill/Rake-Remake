using UnityEngine;

/// <summary>
/// Level skip trigger - loads next level or skips day phase.
/// </summary>
public class LevelSkip : MonoBehaviour
{
    [SerializeField] private string _nextLevelName;
    [SerializeField] private int _nextLevelIndex = -1;
    [SerializeField] private bool _skipDayPhase = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (_skipDayPhase)
            {
                GameTimer.Instance?.SkipDay();
                InformationUI.Instance?.DrawObjective("Day skipped. Night begins...");
            }
            else if (!string.IsNullOrEmpty(_nextLevelName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(_nextLevelName);
            }
            else if (_nextLevelIndex >= 0)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(_nextLevelIndex);
            }
        }
    }
}