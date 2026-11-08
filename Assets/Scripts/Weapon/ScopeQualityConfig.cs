using UnityEngine;

namespace Lake.Weapon
{
    /// <summary>
    /// Конфигурация качества рендера физического прицела.
    /// ScriptableObject для настройки качества на разных машинах.
    /// </summary>
    [CreateAssetMenu(fileName = "ScopeQualityConfig", menuName = "Lake/Weapon/Scope Quality Config")]
    public class ScopeQualityConfig : ScriptableObject
    {
        [Header("Качество Render Texture")]
        [Tooltip("Разрешение Render Texture (1024 = высокое, 512 = среднее, 256 = низкое)")]
        [SerializeField] private int renderResolution = 1024;

        [Tooltip("Формат Render Texture для HDRP")]
        [SerializeField] private RenderTextureFormat renderFormat = RenderTextureFormat.ARGBHalf;

        [Tooltip("Anti-Aliasing (1 = отключено, 2 = MSAA 2x, 4 = MSAA 4x, 8 = MSAA 8x)")]
        [SerializeField] private int antiAliasing = 4;

        [Tooltip("Глубина буфера (16, 24, 32)")]
        [SerializeField] private int depthBuffer = 24;

        [Header("Производительность")]
        [Tooltip("Включать камеру прицела только при прицеливании (рекомендуется)")]
        [SerializeField] private bool enableCameraOnAimOnly = true;

        [Tooltip("Задержка включения камеры (секунды) для синхронизации с анимацией")]
        [SerializeField] private float cameraEnableDelay = 0.05f;

        [Tooltip("Задержка выключения камеры (секунды)")]
        [SerializeField] private float cameraDisableDelay = 0.1f;

        [Header("Параллакс")]
        [Tooltip("Включить параллакс-эффект (смещение изображения при движении головы)")]
        [SerializeField] private bool enableParallax = true;

        [Tooltip("Сила параллакса (0.01 - 0.1)")]
        [SerializeField] [Range(0.01f, 0.1f)] private float parallaxStrength = 0.05f;

        [Tooltip("Скорость сглаживания параллакса")]
        [SerializeField] private float parallaxSmoothSpeed = 8f;

        [Header("Настройки камеры прицела")]
        [Tooltip("FOV камеры прицела (узкий угол для оптического прицела)")]
        [SerializeField] [Range(3f, 15f)] private float scopeCameraFOV = 8f;

        [Tooltip("Near clip plane камеры прицела")]
        [SerializeField] private float nearClipPlane = 0.01f;

        [Tooltip("Far clip plane камеры прицела")]
        [SerializeField] private float farClipPlane = 1000f;

        #region Public Properties

        public int RenderResolution => renderResolution;
        public RenderTextureFormat RenderFormat => renderFormat;
        public int AntiAliasing => antiAliasing;
        public int DepthBuffer => depthBuffer;
        public bool EnableCameraOnAimOnly => enableCameraOnAimOnly;
        public float CameraEnableDelay => cameraEnableDelay;
        public float CameraDisableDelay => cameraDisableDelay;
        public bool EnableParallax => enableParallax;
        public float ParallaxStrength => parallaxStrength;
        public float ParallaxSmoothSpeed => parallaxSmoothSpeed;
        public float ScopeCameraFOV => scopeCameraFOV;
        public float NearClipPlane => nearClipPlane;
        public float FarClipPlane => farClipPlane;

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Создать конфигурацию для высокого качества (High settings).
        /// </summary>
        public static ScopeQualityConfig CreateHighQuality()
        {
            var config = CreateInstance<ScopeQualityConfig>();
            config.renderResolution = 1024;
            config.renderFormat = RenderTextureFormat.ARGBHalf;
            config.antiAliasing = 4;
            config.depthBuffer = 24;
            config.enableParallax = true;
            config.parallaxStrength = 0.05f;
            return config;
        }

        /// <summary>
        /// Создать конфигурацию для среднего качества (Medium settings).
        /// </summary>
        public static ScopeQualityConfig CreateMediumQuality()
        {
            var config = CreateInstance<ScopeQualityConfig>();
            config.renderResolution = 512;
            config.renderFormat = RenderTextureFormat.ARGBHalf;
            config.antiAliasing = 2;
            config.depthBuffer = 24;
            config.enableParallax = true;
            config.parallaxStrength = 0.03f;
            return config;
        }

        /// <summary>
        /// Создать конфигурацию для низкого качества (Low settings).
        /// </summary>
        public static ScopeQualityConfig CreateLowQuality()
        {
            var config = CreateInstance<ScopeQualityConfig>();
            config.renderResolution = 256;
            config.renderFormat = RenderTextureFormat.ARGB4444;
            config.antiAliasing = 1;
            config.depthBuffer = 16;
            config.enableParallax = false;
            config.parallaxStrength = 0f;
            return config;
        }

        #endregion
    }
}
