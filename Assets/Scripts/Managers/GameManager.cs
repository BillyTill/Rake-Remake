using UnityEngine;

/// <summary>
/// Central game manager - coordinates all systems.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    [Header("References")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _rakePrefab;

    private NetworkedPlayer _localPlayer;
    private Enemy_AI _rake;

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

    private void Start()
    {
        // Initialize all managers
        InitializeManagers();
    }

    private void InitializeManagers()
    {
        // Managers initialize themselves via Awake/Start
        // This just ensures they exist
        _ = NotificationsManager.Instance;
        _ = InformationUI.Instance;
        _ = WeaponManager.Instance;
        _ = CameraManager.Instance;
        _ = LevelItemsManager.Instance;
        _ = SaveGameSystem.Instance;
        _ = GameTimeManager.Instance;
        _ = GameTimer.Instance;
        _ = ObjectiveManager.Instance;
        _ = RainManager.Instance;
    }

    public void StartNewGame()
    {
        CurrentState = GameState.Playing;
        
        // Spawn player
        if (_playerPrefab != null)
        {
            var playerObj = Instantiate(_playerPrefab);
            _localPlayer = playerObj.GetComponent<NetworkedPlayer>();
        }

        // Spawn Rake
        if (_rakePrefab != null)
        {
            var rakeObj = Instantiate(_rakePrefab);
            _rake = rakeObj.GetComponent<Enemy_AI>();
        }

        // Start game timer
        if (GameTimer.Instance != null)
        {
            // Timer starts automatically
        }
    }

    public void RegisterLocalPlayer(NetworkedPlayer player)
    {
        _localPlayer = player;
    }

    public void OnPlayerDeath()
    {
        CurrentState = GameState.GameOver;
        // Game over handled by FPSController
    }

    public void OnRakeDeath()
    {
        CurrentState = GameState.Victory;
        InformationUI.Instance?.DrawObjective("You Win! The Rake has been defeated.");
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
        }
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
        }
    }

    public void QuitToMenu()
    {
        CurrentState = GameState.MainMenu;
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}