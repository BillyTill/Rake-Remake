using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pause menu manager.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [SerializeField] private GameObject _pauseMenu;
    [SerializeField] private GameObject _settingsMenu;
    [SerializeField] private GameObject _mainMenuButton;

    private bool _isPaused = false;
    private bool _wasInCameraMode = false;

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
        if (_pauseMenu != null) _pauseMenu.SetActive(false);
        if (_settingsMenu != null) _settingsMenu.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (_isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        if (_isPaused) return;

        _isPaused = true;
        Time.timeScale = 0f;

        // Check if in camera mode
        _wasInCameraMode = CameraComputer.Instance != null && CameraComputer.Instance.IsWatching;

        if (_wasInCameraMode)
        {
            CameraComputer.Instance.Exit();
        }

        FPSController.Instance?.SetPlayerState("PAUSEMENU");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_pauseMenu != null) _pauseMenu.SetActive(true);
    }

    public void Resume()
    {
        if (!_isPaused) return;

        _isPaused = false;
        Time.timeScale = 1f;

        FPSController.Instance?.SetPlayerState("BEFOREPAUSE");
        FPSController.Instance?.SetPlayerState("FULL");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_pauseMenu != null) _pauseMenu.SetActive(false);
        if (_settingsMenu != null) _settingsMenu.SetActive(false);

        // Restore camera mode if was active
        if (_wasInCameraMode && CameraComputer.Instance != null)
        {
            CameraComputer.Instance.Action();
        }
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OpenSettings()
    {
        if (_pauseMenu != null) _pauseMenu.SetActive(false);
        if (_settingsMenu != null) _settingsMenu.SetActive(true);
    }

    public void CloseSettings()
    {
        if (_settingsMenu != null) _settingsMenu.SetActive(false);
        if (_pauseMenu != null) _pauseMenu.SetActive(true);
    }
}