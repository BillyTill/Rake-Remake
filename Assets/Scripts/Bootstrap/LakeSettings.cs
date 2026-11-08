using UnityEngine;

namespace Lake.Bootstrap
{
    /// <summary>
    /// Проектные настройки, которые нужны до загрузки первой сцены.
    /// Живёт в Resources, чтобы бутстраппер находил его без ссылок в сцене.
    /// </summary>
    [CreateAssetMenu(fileName = "LakeSettings", menuName = "Lake/Settings")]
    public sealed class LakeSettings : ScriptableObject
    {
        public const string ResourceName = "LakeSettings";

        [Header("Кадры")]
        [Tooltip("0 — не ограничивать (упирается в vsync или частоту монитора).")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private int vSyncCount = 1;

        [Header("Ввод")]
        [SerializeField] private bool lockCursorOnStart = true;

        [Header("Игрок")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 1.1f, -4f);
        [SerializeField] private Vector3 spawnEulerAngles = Vector3.zero;

        public int TargetFrameRate => targetFrameRate;
        public int VSyncCount => vSyncCount;
        public bool LockCursorOnStart => lockCursorOnStart;
        public GameObject PlayerPrefab => playerPrefab;
        public Vector3 SpawnPosition => spawnPosition;
        public Vector3 SpawnEulerAngles => spawnEulerAngles;

        /// <summary>Создаёт дефолтный ассет, если его ещё нет. Вызывается из меню и в рантайме.</summary>
        public static LakeSettings GetOrCreate()
        {
            var settings = Resources.Load<LakeSettings>(ResourceName);
            if (settings != null)
                return settings;

#if UNITY_EDITOR
            settings = CreateInstance<LakeSettings>();

            const string folder = "Assets/Resources";
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");

            UnityEditor.AssetDatabase.CreateAsset(settings, $"{folder}/{ResourceName}.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"[Lake] Создан дефолтный ассет настроек: {folder}/{ResourceName}.asset");
            return settings;
#else
            Debug.LogError("[Lake] Ассет настроек не найден в Resources. Игра работает с настройками по умолчанию.");
            return CreateInstance<LakeSettings>();
#endif
        }
    }
}