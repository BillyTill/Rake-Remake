using UnityEngine;

/// <summary>
/// Application quit handler.
/// </summary>
public class AppQuit : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Handled by PauseManager in game, quit in menu
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 0)
            {
                Application.Quit();
            }
        }
    }

    private void OnApplicationQuit()
    {
        // Save game on quit
        if (SaveGameSystem.Instance != null)
        {
            SaveGameSystem.Instance.SaveGame();
        }
    }
}