using UnityEngine;

/// <summary>
/// Handles saving and loading game state using PlayerPrefs.
/// Saves: player health, weapons, ammo, camera positions, traps, items, Rake health, sound triggers, screemers, windows.
/// </summary>
public class SaveGameSystem : MonoBehaviour
{
    public static SaveGameSystem Instance { get; private set; }

    [SerializeField] private GameObject _trapPrefab;

    private const string PREFS_LEVEL = "Level:";
    const string PREFS_PLAYER_HEALTH = "PlayerHealth:";
    const string PREFS_RAKE_HEALTH = "RakeHealth:";
    const string PREFS_WEAPONS_COUNT = "WeaponsCount:";
    const string PREFS_WEAPON_PREFIX = "Weapon:";
    const string PREFS_AMMO_PREFIX = "Ammo:";
    const string PREFS_CAMERAS_COUNT = "CamerasCount";
    const string PREFS_CAMERA_PREFIX = "Camera:";
    const string PREFS_TRAPS_COUNT = "TrapsCount";
    const string PREFS_TRAP_PREFIX = "Trap:";
    const string PREFS_ITEMS_COUNT = "ItemsCount";
    const string PREFS_ITEM_PREFIX = "Item:";
    const string PREFS_SCREEMERS_COUNT = "ScreemersCount";
    const string PREFS_SCREEMER_PREFIX = "Screemer:";
    const string PREFS_SOUND_TRIGGERS_COUNT = "SoundTriggersCount";
    const string PREFS_SOUND_TRIGGER_PREFIX = "SoundTrigger:";
    const string PREFS_WINDOW_PREFIX = "Window:";

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
        SaveCurrentLevel();
    }

    /// <summary>
    /// Save complete game state.
    /// </summary>
    public void SaveGame()
    {
        SaveWindows();
        SavePlayerWeapon();
        SavePlayerAmmo();
        SaveGameCameras();
        SaveTrapPositions();
        SaveItems();
        SaveScreemers();
        SaveSoundTriggers();
        SaveRakeHealth();
        SavePlayerHealth();
        SaveCurrentLevel();
        PlayerPrefs.Save();
        Debug.Log("[SaveGameSystem] Game saved successfully");
    }

    /// <summary>
    /// Load complete game state.
    /// </summary>
    public void LoadGame()
    {
        LoadWindows();
        LoadPlayerWeapon();
        LoadPlayerAmmo();
        LoadGameCameras();
        LoadLevelTraps();
        LoadItems();
        LoadScreemers();
        LoadSoundTriggers();
        LoadRakeHealth();
        LoadPlayerHealth();
        Debug.Log("[SaveGameSystem] Game loaded successfully");
    }

    #region Save Methods

    private void SaveCurrentLevel()
    {
        PlayerPrefs.SetInt(PREFS_LEVEL, UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void SaveWindows()
    {
        int count = LevelItemsManager.Instance.WindowsObj.Count;
        for (int i = 0; i < count; i++)
        {
            var window = LevelItemsManager.Instance.WindowsObj[i].GetComponent<Window_obj>();
            if (window != null)
            {
                PlayerPrefs.SetInt(PREFS_WINDOW_PREFIX + i, window.IsBroken ? 1 : 0);
            }
        }
        Debug.Log($"[SaveGameSystem] Saved {count} windows");
    }

    private void SaveRakeHealth()
    {
        var rake = GameObject.FindGameObjectWithTag("Rake");
        if (rake != null)
        {
            var enemyAI = rake.GetComponent<Enemy_AI>();
            if (enemyAI != null)
            {
                PlayerPrefs.SetFloat(PREFS_RAKE_HEALTH, enemyAI.Health);
            }
        }
    }

    private void SavePlayerHealth()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var fpsController = player.GetComponent<FPSController>();
            if (fpsController != null)
            {
                PlayerPrefs.SetInt(PREFS_PLAYER_HEALTH, fpsController.Health);
            }
        }
    }

    private void SaveSoundTriggers()
    {
        LevelItemsManager.Instance.RefreshSoundTriggersList();
        int count = LevelItemsManager.Instance.SoundTriggers.Count;
        PlayerPrefs.SetInt(PREFS_SOUND_TRIGGERS_COUNT, count);
        for (int i = 0; i < count; i++)
        {
            var trigger = LevelItemsManager.Instance.SoundTriggers[i].GetComponent<SoundTrigger>();
            if (trigger != null)
            {
                PlayerPrefs.SetInt(PREFS_SOUND_TRIGGER_PREFIX + i, trigger.ID);
            }
        }
    }

    private void SaveGameCameras()
    {
        int count = CameraManager.Instance.CameraCount;
        PlayerPrefs.SetInt(PREFS_CAMERAS_COUNT, count);
        for (int i = 0; i < count; i++)
        {
            var camTransform = CameraManager.Instance.GetCameraAtIndex(i);
            if (camTransform != null)
            {
                var recordCam = camTransform.parent.GetComponent<RecordCamera_obj>();
                if (recordCam != null)
                {
                    PlayerPrefs.SetInt(PREFS_CAMERA_PREFIX + i, recordCam.CameraID);
                }
            }
        }
    }

    private void SaveScreemers()
    {
        LevelItemsManager.Instance.RefreshScreemersList();
        int count = LevelItemsManager.Instance.Screemers.Count;
        PlayerPrefs.SetInt(PREFS_SCREEMERS_COUNT, count);
        for (int i = 0; i < count; i++)
        {
            var screemer = LevelItemsManager.Instance.Screemers[i].GetComponent<ScreemerSystem>();
            if (screemer != null)
            {
                PlayerPrefs.SetInt(PREFS_SCREEMER_PREFIX + i, screemer.ID);
            }
        }
    }

    private void SaveItems()
    {
        LevelItemsManager.Instance.RefreshItemList();
        int count = LevelItemsManager.Instance.Items.Count;
        PlayerPrefs.SetInt(PREFS_ITEMS_COUNT, count);
        for (int i = 0; i < count; i++)
        {
            var item = LevelItemsManager.Instance.Items[i].GetComponent<Item>();
            if (item != null)
            {
                PlayerPrefs.SetInt(PREFS_ITEM_PREFIX + i, item.ID);
            }
        }
    }

    private void SaveTrapPositions()
    {
        LevelItemsManager.Instance.RefreshTrapList();
        int count = LevelItemsManager.Instance.TrapObjects.Count;
        PlayerPrefs.SetInt(PREFS_TRAPS_COUNT, count);
        for (int i = 0; i < count; i++)
        {
            var transform = LevelItemsManager.Instance.TrapObjects[i].transform;
            PlayerPrefs.SetFloat(PREFS_TRAP_PREFIX + i + "_x", transform.position.x);
            PlayerPrefs.SetFloat(PREFS_TRAP_PREFIX + i + "_y", transform.position.y);
            PlayerPrefs.SetFloat(PREFS_TRAP_PREFIX + i + "_z", transform.position.z);
        }
    }

    private void SavePlayerWeapon()
    {
        int count = WeaponManager.Instance.PlayerWeaponsCount;
        PlayerPrefs.SetInt(PREFS_WEAPONS_COUNT, count);
        for (int i = 0; i < count; i++)
        {
            var weapon = WeaponManager.Instance.GetWeaponAtIndex(i);
            if (weapon != null)
            {
                PlayerPrefs.SetInt(PREFS_WEAPON_PREFIX + i, weapon.WeaponID);
            }
        }
    }

    private void SavePlayerAmmo()
    {
        for (int i = 0; i < WeaponManager.Instance.WeaponsAmmoCount; i++)
        {
            PlayerPrefs.SetInt(PREFS_AMMO_PREFIX + i, WeaponManager.Instance.GetAmmo(i));
        }
    }

    #endregion

    #region Load Methods

    private void LoadWindows()
    {
        int count = LevelItemsManager.Instance.WindowsObj.Count;
        for (int i = 0; i < count; i++)
        {
            if (PlayerPrefs.GetInt(PREFS_WINDOW_PREFIX + i) == 1)
            {
                var window = LevelItemsManager.Instance.WindowsObj[i].GetComponent<Window_obj>();
                if (window != null)
                {
                    window.SetBrokenState();
                }
            }
        }
        Debug.Log($"[SaveGameSystem] Loaded {count} windows");
    }

    private void LoadRakeHealth()
    {
        var rake = GameObject.FindGameObjectWithTag("Rake");
        if (rake != null)
        {
            var enemyAI = rake.GetComponent<Enemy_AI>();
            if (enemyAI != null)
            {
                enemyAI.Health = Mathf.RoundToInt(PlayerPrefs.GetFloat(PREFS_RAKE_HEALTH, 2500f));
            }
        }
    }

    private void LoadPlayerHealth()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var fpsController = player.GetComponent<FPSController>();
            if (fpsController != null)
            {
                fpsController.Health = PlayerPrefs.GetInt(PREFS_PLAYER_HEALTH, 100);
            }
        }
    }

    private void LoadSoundTriggers()
    {
        int count = PlayerPrefs.GetInt(PREFS_SOUND_TRIGGERS_COUNT, 0);
        LevelItemsManager.Instance.RefreshSoundTriggersList();

        // Deactivate all first
        foreach (var trigger in LevelItemsManager.Instance.SoundTriggers)
        {
            if (trigger != null) trigger.SetActive(false);
        }

        // Activate saved ones
        for (int i = 0; i < count; i++)
        {
            int id = PlayerPrefs.GetInt(PREFS_SOUND_TRIGGER_PREFIX + i, -1);
            if (id >= 0 && id < LevelItemsManager.Instance.SoundTriggers.Count)
            {
                LevelItemsManager.Instance.SoundTriggers[id].SetActive(true);
            }
        }
    }

    private void LoadScreemers()
    {
        int count = PlayerPrefs.GetInt(PREFS_SCREEMERS_COUNT, 0);
        LevelItemsManager.Instance.RefreshScreemersList();

        // Deactivate all first
        foreach (var screemer in LevelItemsManager.Instance.Screemers)
        {
            if (screemer != null) screemer.SetActive(false);
        }

        // Activate saved ones
        for (int i = 0; i < count; i++)
        {
            int id = PlayerPrefs.GetInt(PREFS_SCREEMER_PREFIX + i, -1);
            if (id >= 0 && id < LevelItemsManager.Instance.Screemers.Count)
            {
                LevelItemsManager.Instance.Screemers[id].SetActive(true);
            }
        }
    }

    private void LoadItems()
    {
        int count = PlayerPrefs.GetInt(PREFS_ITEMS_COUNT, 0);
        LevelItemsManager.Instance.RefreshItemList();

        // Deactivate all first
        foreach (var item in LevelItemsManager.Instance.Items)
        {
            if (item != null) item.SetActive(false);
        }

        // Activate saved ones
        for (int i = 0; i < count; i++)
        {
            int id = PlayerPrefs.GetInt(PREFS_ITEM_PREFIX + i, -1);
            if (id >= 0 && id < LevelItemsManager.Instance.Items.Count)
            {
                LevelItemsManager.Instance.Items[id].SetActive(true);
            }
        }
    }

    private void LoadLevelTraps()
    {
        LevelItemsManager.Instance.TrapObjects.Clear();
        int count = PlayerPrefs.GetInt(PREFS_TRAPS_COUNT, 0);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = Vector3.zero;
            pos.x = PlayerPrefs.GetFloat(PREFS_TRAP_PREFIX + i + "_x", 0f);
            pos.y = PlayerPrefs.GetFloat(PREFS_TRAP_PREFIX + i + "_y", 0f) + 1f; // Slight offset to prevent ground clipping
            pos.z = PlayerPrefs.GetFloat(PREFS_TRAP_PREFIX + i + "_z", 0f);

            if (_trapPrefab != null)
            {
                var obj = Instantiate(_trapPrefab, pos, _trapPrefab.transform.rotation);
                LevelItemsManager.Instance.AddTrap(obj);
            }
        }
    }

    private void LoadGameCameras()
    {
        CameraManager.Instance.ClearCameras();
        int count = PlayerPrefs.GetInt(PREFS_CAMERAS_COUNT, 0);

        for (int i = 0; i < count; i++)
        {
            int cameraID = PlayerPrefs.GetInt(PREFS_CAMERA_PREFIX + i, -1);
            if (cameraID >= 0 && cameraID < CameraManager.Instance.AllCamerasCount)
            {
                var camTransform = CameraManager.Instance.GetAllCameraAtIndex(cameraID);
                if (camTransform != null)
                {
                    var recordCam = camTransform.GetComponent<RecordCamera_obj>();
                    if (recordCam != null)
                    {
                        recordCam.SetupCamera();
                    }
                }
            }
        }
    }

    private void LoadPlayerWeapon()
    {
        WeaponManager.Instance.ClearPlayerWeapons();
        int count = PlayerPrefs.GetInt(PREFS_WEAPONS_COUNT, 0);

        for (int i = 0; i < count; i++)
        {
            int weaponID = PlayerPrefs.GetInt(PREFS_WEAPON_PREFIX + i, -1);
            if (weaponID >= 0 && weaponID < WeaponManager.Instance.AllWeaponsCount)
            {
                WeaponManager.Instance.AddWeapon(weaponID, 0, false);
            }
        }
    }

    private void LoadPlayerAmmo()
    {
        for (int i = 0; i < WeaponManager.Instance.WeaponsAmmoCount; i++)
        {
            int ammo = PlayerPrefs.GetInt(PREFS_AMMO_PREFIX + i, 0);
            WeaponManager.Instance.SetAmmo(i, ammo);
        }
    }

    #endregion
}