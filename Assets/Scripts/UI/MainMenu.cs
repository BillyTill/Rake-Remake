using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu controller.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _creditsPanel;
    [SerializeField] private string _gameSceneName = "GameScene";
    [SerializeField] private int _gameSceneIndex = 1;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        ShowMainMenu();
    }

    public void StartGame()
    {
        SceneManager.LoadScene(_gameSceneIndex);
    }

    public void ContinueGame()
    {
        // Load saved game
        SceneManager.LoadScene(_gameSceneIndex);
        // SaveGameSystem will auto-load in FPSController.Start
    }

    public void OpenSettings()
    {
        _mainMenuPanel?.SetActive(false);
        _settingsPanel?.SetActive(true);
    }

    public void OpenCredits()
    {
        _mainMenuPanel?.SetActive(false);
        _creditsPanel?.SetActive(true);
    }

    public void CloseSettings()
    {
        _settingsPanel?.SetActive(false);
        _mainMenuPanel?.SetActive(true);
    }

    public void CloseCredits()
    {
        _creditsPanel?.SetActive(false);
        _mainMenuPanel?.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void ShowMainMenu()
    {
        _mainMenuPanel?.SetActive(true);
        _settingsPanel?.SetActive(false);
        _creditsPanel?.SetActive(false);
    }
}