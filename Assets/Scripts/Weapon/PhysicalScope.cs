using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Lake.Weapon
{
    /// <summary>
    /// Физический прицел с Render Texture (PiP Scope) для Unity 6 HDRP.
    /// 
    /// Игрок смотрит через реальную линзу оптики с рендер-текстурой,
    /// а не через UI-оверлей или изменение FOV основной камеры.
    /// 
    /// Ключевые особенности:
    /// - Render Texture с HDR форматом (ARGBHalf) для корректного HDRP рендеринга
    /// - Отдельная Scope Camera с Physical Camera настройками
    /// - Shader Graph для линзы с параллаксом и виньеткой
    /// - Оптимизация: камера включается только при прицеливании
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PhysicalScope : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Основные ссылки")]
        [Tooltip("Камера прицела, которая рендерит в Render Texture")]
        [SerializeField] private Camera scopeCamera;

        [Tooltip("Render Texture для прицела (должна быть создана с HDR форматом)")]
        [SerializeField] private RenderTexture scopeRenderTexture;

        [Tooltip("Animator оружия с параметром IsAiming")]
        [SerializeField] private Animator weaponAnimator;

        [Header("Настройки прицеливания")]
        [Tooltip("Имя кнопки прицеливания (по умолчанию Fire2 = ПКМ)")]
        [SerializeField] private string aimButton = "Fire2";

        [Tooltip("Имя bool-параметра в Animator Controller")]
        [SerializeField] private string aimAnimatorBool = "IsAiming";

        [Tooltip("Время анимации прицеливания (секунды)")]
        [SerializeField] private float aimTransitionTime = 0.15f;

        [Header("Настройки шейдера")]
        [Tooltip("Имя свойства текстуры в шейдере (по умолчанию _ScopeTexture)")]
        [SerializeField] private string texturePropertyName = "_ScopeTexture";

        [Tooltip("Имя свойства параллакса в шейдере")]
        [SerializeField] private string parallaxPropertyName = "_ScopeParallaxOffset";

        [Tooltip("Сила параллакс-эффекта (смещение при движении головы)")]
        [SerializeField] private float parallaxStrength = 0.05f;

        [Tooltip("Скорость сглаживания параллакса")]
        [SerializeField] private float parallaxSmoothSpeed = 8f;

        [Header("Оптимизация")]
        [Tooltip("Включать камеру прицела только при прицеливании")]
        [SerializeField] private bool enableScopeCameraOnAim = true;

        [Tooltip("Задержка включения камеры после начала анимации (для синхронизации)")]
        [SerializeField] private float cameraEnableDelay = 0.05f;

        [Tooltip("Задержка выключения камеры после окончания прицеливания")]
        [SerializeField] private float cameraDisableDelay = 0.1f;

        #endregion

        #region Public Properties

        /// <summary>
        /// Текущее состояние прицеливания.
        /// </summary>
        public bool IsAiming { get; private set; }

        /// <summary>
        /// Нормализованное значение прицеливания (0..1) для внешних систем.
        /// </summary>
        public float AimNormalized { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Событие изменения состояния прицеливания.
        /// Параметр: true = начали прицеливаться, false = закончили.
        /// </summary>
        public event System.Action<bool> OnAimStateChanged;

        #endregion

        #region Private Fields

        private Camera mainCamera;
        private Transform scopeLensTransform; // Трансформ линзы окуляра
        private Vector2 currentParallaxOffset;
        private Vector2 targetParallaxOffset;
        private float cameraToggleTimer;
        private bool pendingCameraEnable;
        private bool pendingCameraDisable;
        private int texturePropertyID;
        private int parallaxPropertyID;
        private HDAdditionalCameraData scopeCameraHDData;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Кэшируем главную камеру
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[PhysicalScope] Main Camera не найдена! Убедитесь, что камера имеет тег MainCamera.");
                enabled = false;
                return;
            }

            // Проверяем ссылки
            ValidateReferences();

            // Кэшируем Property ID для производительности
            texturePropertyID = Shader.PropertyToID(texturePropertyName);
            parallaxPropertyID = Shader.PropertyToID(parallaxPropertyName);
        }

        private void Start()
        {
            InitializeScopeCamera();
            InitializeRenderTexture();
            FindScopeLensTransform();
        }

        private void Update()
        {
            HandleInput();
            UpdateAimState();
            UpdateParallax();
            UpdateCameraToggle();
        }

        private void OnDestroy()
        {
            // Очистка
            if (scopeRenderTexture != null && scopeCamera != null)
            {
                scopeCamera.targetTexture = null;
            }
        }

        #endregion

        #region Initialization

        private void ValidateReferences()
        {
            if (scopeCamera == null)
            {
                Debug.LogError("[PhysicalScope] Scope Camera не назначена!", this);
                enabled = false;
                return;
            }

            if (scopeRenderTexture == null)
            {
                Debug.LogError("[PhysicalScope] Render Texture не назначена! Создайте RT_WeaponScope с HDR форматом.", this);
                enabled = false;
                return;
            }

            if (weaponAnimator == null)
            {
                Debug.LogWarning("[PhysicalScope] Animator не назначен. Анимация прицеливания не будет работать.", this);
            }
        }

        private void InitializeScopeCamera()
        {
            if (scopeCamera == null)
                return;

            // Получаем HD Additional Camera Data
            scopeCameraHDData = scopeCamera.GetComponent<HDAdditionalCameraData>();
            if (scopeCameraHDData == null)
            {
                Debug.LogWarning("[PhysicalScope] HDAdditionalCameraData не найден на Scope Camera. Добавьте компонент для корректной работы HDRP.", scopeCamera);
                scopeCameraHDData = scopeCamera.gameObject.AddComponent<HDAdditionalCameraData>();
            }

            // Настройка Custom Frame Settings для HDRP
            scopeCameraHDData.customRenderingSettings = true;
            
            // Убеждаемся, что Volume и Light Layers включены
            var frameSettings = scopeCameraHDData.renderingPathCustomFrameSettings;
            frameSettings.SetEnabled(FrameSettingsField.Volumetrics, true);
            frameSettings.SetEnabled(FrameSettingsField.BigTilePrepass, true);
            frameSettings.SetEnabled(FrameSettingsField.Postprocess, true);
            
            scopeCameraHDData.renderingPathCustomFrameSettings = frameSettings;

            // Physical Camera настройки (узкий FOV как у реального прицела)
            scopeCamera.usePhysicalProperties = true;
            scopeCamera.fieldOfView = 8f; // Узкий угол, как у оптического прицела (4x-8x zoom)

            // Назначаем Render Texture
            scopeCamera.targetTexture = scopeRenderTexture;

            // КАМЕРА ОТКЛЮЧЕНА ПО УМОЛЧАНИЮ (критично для производительности)
            scopeCamera.enabled = false;

            Debug.Log("[PhysicalScope] Scope Camera инициализирована. FOV: " + scopeCamera.fieldOfView);
        }

        private void InitializeRenderTexture()
        {
            if (scopeRenderTexture == null)
                return;

            // Проверяем формат (должен быть HDR для HDRP)
            if (scopeRenderTexture.format != RenderTextureFormat.ARGBHalf)
            {
                Debug.LogWarning($"[PhysicalScope] Render Texture формат {scopeRenderTexture.format} не является HDR. " +
                               $"Рекомендуется ARGBHalf для корректной работы HDRP (bloom, tonemapping).");
            }

            // Передаём текстуру в шейдер ГЛОБАЛЬНО (один раз при старте)
            // Это позволяет использовать один материал на всех экземплярах оружия
            Shader.SetGlobalTexture(texturePropertyID, scopeRenderTexture);

            Debug.Log($"[PhysicalScope] Render Texture назначена в шейдер: {texturePropertyName}");
        }

        private void FindScopeLensTransform()
        {
            // Пытаемся найти линзу окуляра в иерархии
            // Ожидаем, что меш линзы называется "Lens", "Ocular" или "ScopeLens"
            var lensTransforms = GetComponentsInChildren<Transform>();
            foreach (var t in lensTransforms)
            {
                if (t.name.Contains("Lens") || t.name.Contains("Ocular") || t.name.Contains("Scope"))
                {
                    scopeLensTransform = t;
                    Debug.Log($"[PhysicalScope] Найдена линза окуляра: {t.name}");
                    break;
                }
            }

            if (scopeLensTransform == null)
            {
                Debug.LogWarning("[PhysicalScope] Линза окуляра не найдена в иерархии. Параллакс не будет работать. " +
                               "Назовите меш линзы 'Lens', 'Ocular' или 'ScopeLens'.");
            }
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            // Обработка нажатия ПКМ (Fire2)
            if (Input.GetButtonDown(aimButton))
            {
                StartAim();
            }
            else if (Input.GetButtonUp(aimButton))
            {
                StopAim();
            }
        }

        private void StartAim()
        {
            if (IsAiming)
                return;

            IsAiming = true;
            AimNormalized = 0f;

            // Запускаем анимацию прицеливания
            if (weaponAnimator != null)
            {
                weaponAnimator.SetBool(aimAnimatorBool, true);
            }

            // Планируем включение камеры с задержкой
            pendingCameraEnable = true;
            pendingCameraDisable = false;
            cameraToggleTimer = cameraEnableDelay;

            // Уведомляем подписчиков
            OnAimStateChanged?.Invoke(true);

            Debug.Log("[PhysicalScope] Начало прицеливания");
        }

        private void StopAim()
        {
            if (!IsAiming)
                return;

            IsAiming = false;

            // Останавливаем анимацию прицеливания
            if (weaponAnimator != null)
            {
                weaponAnimator.SetBool(aimAnimatorBool, false);
            }

            // Планируем выключение камеры с задержкой
            pendingCameraEnable = false;
            pendingCameraDisable = true;
            cameraToggleTimer = cameraDisableDelay;

            // Уведомляем подписчиков
            OnAimStateChanged?.Invoke(false);

            Debug.Log("[PhysicalScope] Конец прицеливания");
        }

        /// <summary>
        /// Принудительно установить состояние прицеливания (для внешних систем).
        /// </summary>
        public void ForceAim(bool state)
        {
            if (state)
            {
                StartAim();
            }
            else
            {
                StopAim();
            }
        }

        #endregion

        #region Aim State Update

        private void UpdateAimState()
        {
            if (!IsAiming)
            {
                AimNormalized = Mathf.Lerp(AimNormalized, 0f, Time.deltaTime * (1f / aimTransitionTime));
                return;
            }

            // Плавное нарастание AimNormalized
            AimNormalized = Mathf.Lerp(AimNormalized, 1f, Time.deltaTime * (1f / aimTransitionTime));
        }

        #endregion

        #region Camera Management

        private void UpdateCameraToggle()
        {
            if (!enableScopeCameraOnAim)
                return;

            // Обработка задержки включения камеры
            if (pendingCameraEnable)
            {
                cameraToggleTimer -= Time.deltaTime;
                if (cameraToggleTimer <= 0f)
                {
                    EnableScopeCamera(true);
                    pendingCameraEnable = false;
                }
            }

            // Обработка задержки выключения камеры
            if (pendingCameraDisable)
            {
                cameraToggleTimer -= Time.deltaTime;
                if (cameraToggleTimer <= 0f)
                {
                    EnableScopeCamera(false);
                    pendingCameraDisable = false;
                }
            }
        }

        private void EnableScopeCamera(bool enable)
        {
            if (scopeCamera == null)
                return;

            scopeCamera.enabled = enable;

            // Синхронизируем позицию и поворот с главной камерой
            if (enable && mainCamera != null)
            {
                scopeCamera.transform.position = mainCamera.transform.position;
                scopeCamera.transform.rotation = mainCamera.transform.rotation;
            }

            Debug.Log($"[PhysicalScope] Scope Camera: {(enable ? "ВКЛ" : "ВЫКЛ")}");
        }

        #endregion

        #region Parallax Effect

        private void UpdateParallax()
        {
            if (scopeLensTransform == null || mainCamera == null)
                return;

            // Вычисляем смещение камеры игрока относительно линзы прицела
            Vector3 lensPos = scopeLensTransform.position;
            Vector3 cameraPos = mainCamera.transform.position;

            // Вектор от линзы к камере в локальных координатах линзы
            Vector3 localOffset = scopeLensTransform.InverseTransformPoint(cameraPos);

            // Нормализуем и применяем силу параллакса
            targetParallaxOffset = new Vector2(
                localOffset.x * parallaxStrength,
                localOffset.y * parallaxStrength
            );

            // Плавное сглаживание параллакса (без дёрганья)
            currentParallaxOffset = Vector2.Lerp(
                currentParallaxOffset,
                targetParallaxOffset,
                Time.deltaTime * parallaxSmoothSpeed
            );

            // Передаём в шейдер
            Shader.SetGlobalVector(parallaxPropertyID, currentParallaxOffset);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Получить текущее смещение параллакса (для отладки).
        /// </summary>
        public Vector2 GetParallaxOffset()
        {
            return currentParallaxOffset;
        }

        /// <summary>
        /// Установить качество рендера прицела (разрешение).
        /// </summary>
        public void SetRenderQuality(int resolution)
        {
            if (scopeRenderTexture == null)
                return;

            // Освобождаем старую текстуру
            scopeRenderTexture.Release();

            // Меняем разрешение
            scopeRenderTexture.width = resolution;
            scopeRenderTexture.height = resolution;

            // Создаём заново
            scopeRenderTexture.Create();

            Debug.Log($"[PhysicalScope] Разрешение Render Texture изменено на {resolution}x{resolution}");
        }

        #endregion

        #region Editor Debug

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (scopeLensTransform == null || mainCamera == null)
                return;

            // Рисуем линию от линзы к камере (для визуализации параллакса)
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(scopeLensTransform.position, mainCamera.transform.position);

            // Рисуем направление взгляда прицела
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(scopeLensTransform.position, scopeLensTransform.forward * 2f);
        }
#endif

        #endregion
    }
}
