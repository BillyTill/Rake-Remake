using System.Collections;
using UnityEngine;

/// <summary>
/// Main player controller - central hub for all player systems.
/// Singleton pattern for global access.
/// </summary>
public class FPSController : MonoBehaviour
{
    public enum PlayerState
    {
        Full,       // Normal gameplay
        Freezed,    // Movement frozen (e.g., Rake attack)
        Disabled    // Completely disabled (inventory, camera mode, pause)
    }

    [Header("Components")]
    [SerializeField] private Camera _mainCamera;          // World camera
    [SerializeField] private Camera _weaponCamera;        // View model camera (hands/weapon)
    [SerializeField] private Light _flashlight;
    [SerializeField] private AudioListener _audioListener;
    [SerializeField] private GameObject _playerCanvas;    // HUD canvas
    [SerializeField] private Animator _cameraAnimator;    // Camera effects (blood, shake)
    [SerializeField] private Animator _effectAnimator;    // Screen effects
    [SerializeField] private Animator _itemInfoAnimator;  // Item info UI

    [Header("References")]
    [SerializeField] private WeaponManager _weaponManager;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private CameraMouseLook _cameraMouseLook;
    [SerializeField] private FPSCamera _fpsCamera;
    [SerializeField] private Inventory _inventory;

    [Header("Health")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _health = 100;
    [SerializeField] private RectTransform _healthBar;
    [SerializeField] private AudioClip[] _painSounds;

    [Header("Pain Effects")]
    [SerializeField] private AudioClip _staminaSound;

    [Header("State")]
    public PlayerState State { get; private set; } = PlayerState.Full;
    public bool IsHidden { get; set; } = false; // Hiding in locker/wardrobe

    public static FPSController Instance { get; private set; }

    // Properties
    public int Health
    {
        get => _health;
        set
        {
            _health = Mathf.Clamp(value, 0, _maxHealth);
            UpdateHealthUI();
        }
    }

    public int MaxHealth => _maxHealth;
    public Camera MainCamera => _mainCamera;
    public Camera WeaponCamera => _weaponCamera;
    public WeaponManager WeaponManager => _weaponManager;
    public PlayerMovement PlayerMovement => _playerMovement;
    public CameraMouseLook CameraMouseLook => _cameraMouseLook;
    public Inventory Inventory => _inventory;

    private GameObject _loadScreen;

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
        // Find load screen
        _loadScreen = GameObject.Find("LoadScreen");
        if (_loadScreen != null)
        {
            _loadScreen.SetActive(true);
        }

        // Initialize components
        if (_healthBar != null)
        {
            UpdateHealthUI();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        yield return new WaitForSeconds(1f);

        // Load save game
        if (SaveGameSystem.Instance != null)
        {
            SaveGameSystem.Instance.LoadGame();
        }

        yield return new WaitForSeconds(1f);

        // Remove load screen
        if (_effectAnimator != null) _effectAnimator.enabled = true;
        if (_audioListener != null) _audioListener.enabled = true;
        if (_loadScreen != null) Destroy(_loadScreen);
    }

    private void Update()
    {
        // Update health bar
        UpdateHealthUI();

        // Inventory toggle
        if (Input.GetButtonDown("Inventory"))
        {
            ToggleInventory();
        }

        // Flashlight toggle
        if (Input.GetButtonDown("Flashlight"))
        {
            ToggleFlashlight();
        }
    }

    private void UpdateHealthUI()
    {
        if (_healthBar != null)
        {
            float ratio = _maxHealth > 0 ? (float)_health / _maxHealth : 0f;
            _healthBar.sizeDelta = new Vector2(ratio * 200f, 10f);
        }
    }

    private void ToggleInventory()
    {
        if (_inventory == null) return;

        if (!Inventory.IsOpen)
        {
            _inventory.OpenInventory();
            _weaponManager.HideCurrentWeapon();
            _cameraMouseLook.CanControl = false;
            _playerMovement.CanMove = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            State = PlayerState.Disabled;
        }
        else
        {
            _inventory.CloseInventory();
            _cameraMouseLook.CanControl = true;
            _playerMovement.CanMove = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            State = PlayerState.Full;
        }
    }

    private void ToggleFlashlight()
    {
        if (_flashlight != null)
        {
            _flashlight.enabled = !_flashlight.enabled;
        }
    }

    /// <summary>
    /// Force the flashlight on or off.
    /// </summary>
    public void SetLightState(bool on)
    {
        if (_flashlight != null)
        {
            _flashlight.enabled = on;
        }
    }

    /// <summary>
    /// Called when player selects a weapon from inventory.
    /// </summary>
    public void OnWeaponSelected()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Inventory?.CloseInventory();
        _playerMovement.Unfreeze();
    }

    /// <summary>
    /// Enable/disable both cameras (world + view model).
    /// </summary>
    public void SetCamerasEnabled(bool enabled)
    {
        if (_mainCamera != null) _mainCamera.enabled = enabled;
        if (_weaponCamera != null) _weaponCamera.enabled = enabled;
    }

    /// <summary>
    /// Hide current weapon (used when entering camera mode, Rake attack, etc).
    /// </summary>
    public void HideWeapon()
    {
        _weaponManager.HideCurrentWeapon();
        if (_inventory != null && _inventory.IsOpen)
        {
            _inventory.CloseInventory();
        }
    }

    /// <summary>
    /// Completely disable player (camera mode, death, etc).
    /// </summary>
    public void DisablePlayer()
    {
        HideWeapon();
        _playerCanvas.SetActive(false);
        _fpsCamera.enabled = false;
        _playerMovement.CanMove = false;
        _cameraMouseLook.CanControl = false;
        _itemInfoAnimator.SetBool("Show", false);
        _audioListener.enabled = false;
        SetCamerasEnabled(false);
        _inventory.enabled = false;
        _weaponManager.enabled = false;
    }

    /// <summary>
    /// Disable only movement and look (pause menu, etc).
    /// </summary>
    public void DisableMovement()
    {
        _cameraMouseLook.CanControl = false;
        _playerMovement.CanMove = false;
    }

    /// <summary>
    /// Enable movement and look.
    /// </summary>
    public void EnableMovement()
    {
        State = PlayerState.Full;
        _cameraMouseLook.CanControl = true;
        _playerMovement.CanMove = true;
    }

    /// <summary>
    /// Set player state with full control over components.
    /// </summary>
    public void SetPlayerState(string stateName)
    {
        switch (stateName)
        {
            case "INVENTORY":
                _playerMovement.CanMove = false;
                _cameraMouseLook.CanControl = false;
                State = PlayerState.Disabled;
                break;

            case "CAMERAMODE":
                HideWeapon();
                _playerCanvas.SetActive(false);
                _fpsCamera.enabled = false;
                _playerMovement.CanMove = false;
                _cameraMouseLook.CanControl = false;
                _itemInfoAnimator.SetBool("Show", false);
                _audioListener.enabled = false;
                SetCamerasEnabled(false);
                _inventory.enabled = false;
                _weaponManager.enabled = false;
                break;

            case "SCREEMER":
                _cameraMouseLook.CanControl = false;
                _playerMovement.CanMove = false;
                _inventory.enabled = false;
                State = PlayerState.Disabled;
                break;

            case "PAUSEMENU":
                HideWeapon();
                State = PlayerState.Disabled;
                _inventory.enabled = false;
                break;

            case "BEFOREPAUSE":
                State = PlayerState.Full;
                _inventory.enabled = true;
                _weaponManager.enabled = true;
                break;

            case "FULL":
                State = PlayerState.Full;
                _inventory.enabled = true;
                _playerCanvas.SetActive(true);
                _audioListener.enabled = true;
                _fpsCamera.enabled = true;
                _playerMovement.CanMove = true;
                _cameraMouseLook.CanControl = true;
                _cameraMouseLook.CanControl = true;
                _playerMovement.CanMove = true;
                _itemInfoAnimator.SetBool("Show", true);
                SetCamerasEnabled(true);
                _weaponManager.enabled = true;
                break;
        }
    }

    /// <summary>
    /// Enable full player control.
    /// </summary>
    public void EnablePlayer()
    {
        State = PlayerState.Full;
        _inventory.enabled = true;
        _playerCanvas.SetActive(true);
        _audioListener.enabled = true;
        _fpsCamera.enabled = true;
        _playerMovement.CanMove = true;
        _cameraMouseLook.CanControl = true;
        _itemInfoAnimator.SetBool("Show", true);
        SetCamerasEnabled(true);
        _weaponManager.enabled = true;
    }

    /// <summary>
    /// Apply damage to player.
    /// </summary>
    public void TakeDamage(int amount, string damageType)
    {
        if (_health <= 0) return;

        _health -= amount;
        _health = Mathf.Max(0, _health);
        UpdateHealthUI();

        // Play pain sound
        if (_painSounds != null && _painSounds.Length > 0)
        {
            var source = GetComponent<AudioSource>();
            if (source != null)
                source.PlayOneShot(_painSounds[Random.Range(0, _painSounds.Length)]);
        }

        // Trigger specific effects
        switch (damageType)
        {
            case "TrapDamage":
                StartCoroutine(TrapDamageEffect());
                break;
            case "RakeKick":
                StartCoroutine(KickEffect());
                break;
            case "RakeUppercut":
                StartCoroutine(UppercutEffect());
                break;
        }

        if (_health <= 0)
        {
            KillPlayer();
        }
    }

    private IEnumerator TrapDamageEffect()
    {
        _effectAnimator.SetBool("Blood", true);
        _cameraAnimator.SetBool("LegHit", true);
        yield return new WaitForSeconds(1.5f);
        _cameraAnimator.SetBool("LegHit", false);
        yield return new WaitForSeconds(3f);
        _effectAnimator.SetBool("Blood", false);
    }

    private IEnumerator KickEffect()
    {
        _effectAnimator.SetBool("Blood", true);
        _cameraAnimator.SetBool("Kick", true);
        yield return new WaitForSeconds(4.5f);
        _cameraAnimator.SetBool("Kick", false);
        _effectAnimator.SetBool("Blood", false);
    }

    private IEnumerator UppercutEffect()
    {
        _effectAnimator.SetBool("Blood", true);
        _cameraAnimator.SetBool("Falldown", true);
        yield return new WaitForSeconds(4.5f);
        _cameraAnimator.SetBool("Falldown", false);
        _effectAnimator.SetBool("Blood", false);
    }

    /// <summary>
    /// Kill player (game over).
    /// </summary>
    public void KillPlayer()
    {
        _cameraAnimator.SetFloat("Speed", 0f);
        _cameraAnimator.SetBool("Dead", true);
        _weaponManager.HideCurrentWeapon();
        _playerMovement.CanMove = false;
        _cameraMouseLook.CanControl = false;
        InformationUI.Instance?.DrawObjective("Game Over");

        StartCoroutine(RestartGame());
    }

    private IEnumerator RestartGame()
    {
        yield return new WaitForSeconds(5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}