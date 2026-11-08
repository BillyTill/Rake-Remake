using UnityEngine;

namespace Lake.Bodycam.Config
{
    /// <summary>
    /// Конфиг наведения и взгляда. Покрывает параметры PlayerLook.
    /// Дефолты взяты из PlayerLook.cs (строки 16-35, 57-65).
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponLookConfig", menuName = "Lake/Bodycam/Weapon Look Config")]
    public class WeaponLookConfig : ScriptableObject
    {
        [Header("Sensitivity & Input")]
        [Tooltip("Множитель дельты мыши для старого Input Manager (Input.GetAxis). PlayerInputRouter.lookSensitivity(0.12) × _mouseAxisScale(12) = 1.44. НЕ используется в новом Input System.")]
        [SerializeField] private float legacyMouseSensitivity = 1.44f;

        [Tooltip("Коэффициент конвертации screen pixels → mouse counts для нового Input System. На 800 DPI / 1920px ~5.3 counts/pixel. Подбирается эмпирически так, чтобы 100 px давали тот же yaw, что и старый путь.")]
        [SerializeField] private float pixelToCountFactor = 5.3f;

        [Tooltip("Инвертировать вертикаль. PlayerLook.invertLookY (default false).")]
        [SerializeField] private bool invertY = false;

        [Tooltip("Макс. градусов за кадр (защита от телепорта мыши).")]
        [SerializeField] private float maxYawPerFrame = 720f;

        [Tooltip("Сглаживание ввода 0..1 (0 = без сглаживания). Для нового Input System можно добавить easing.")]
        [SerializeField] private float inputSmoothing = 0.0f;

        [Header("Aim Behavior (PlayerLook)")]
        [Tooltip("Мёртвая зона прицела в градусах. PlayerLook.catchUpAngle = 12f.")]
        [SerializeField] private float deadZoneAngle = 12f;

        [Tooltip("Ширина разгонной полосы догона в градусах. PlayerLook.catchUpRampDeg = 12f.")]
        [SerializeField] private float catchUpBand = 12f;

        [Tooltip("Скорость догона в разгонной полосе (gain). PlayerLook.catchUpGain = 9f.")]
        [SerializeField] private float catchUpSpeed = 9f;

        [Tooltip("Макс. скорость догона (градусов/сек), кламп шага. PlayerLook.maxCatchUpSpeed = 220f.")]
        [SerializeField] private float maxCatchUpSpeed = 220f;

        [Tooltip("Жёсткость привязки камеры к прицелу при ADS (0..1). PlayerLook.AdsRigidity (ставит оружие, default 0.8 в прицеле).")]
        [SerializeField] private float adsRigidity = 0.8f;

        [Header("Reticle (PlayerLook)")]
        [Tooltip("Макс. смещение прицела от центра (градусы). PlayerLook.maxReticleOffset = 28f.")]
        [SerializeField] private float maxReticleOffset = 28f;

        [Tooltip("Плавно возвращает прицел к центру. PlayerLook.returnToCenter = false.")]
        [SerializeField] private bool returnToCenter = false;

        [Tooltip("Скорость возврата прицела к центру. PlayerLook.returnSpeed = 2.5f.")]
        [SerializeField] private float returnSpeed = 2.5f;

        [Header("Pitch Limits")]
        [Tooltip("Мин. питч (вниз). PlayerLook.pitchLimit = 85f (кламп -85..85).")]
        [SerializeField] private float minPitch = -85f;

        [Tooltip("Макс. питч (вверх).")]
        [SerializeField] private float maxPitch = 85f;

        [Header("Aim Source (задел под Фазу 2)")]
        [Tooltip("Смещение точки прицела от головы (локально).")]
        [SerializeField] private Vector3 aimOriginOffset = new Vector3(0.15f, -0.1f, 0.2f);

        [Tooltip("Базовый FOV камеры. PlayerWeapons._baseFov = 60f (Camera.fieldOfView).")]
        [SerializeField] private float baseFov = 60f;

        [Tooltip("Множитель FOV при ADS. PlayerWeapons: FOV = baseFov / adsZoom (Rifle adsZoom = 3.5 → ~17°). Здесь множитель = 1/adsZoom.")]
        [SerializeField] private float adsFovMultiplier = 0.285f; // 1/3.5

        // Public getters
        /// <summary>Legacy multiplier: lookSensitivity(0.12) × _mouseAxisScale(12) = 1.44.</summary>
        public float LegacyMouseSensitivity => legacyMouseSensitivity;

        /// <summary>Screen pixels → mouse counts. ~5.3 на 800 DPI / 1920px. Подбирается эмпирически.</summary>
        public float PixelToCountFactor => pixelToCountFactor;

        public bool InvertY => invertY;
        public float MaxYawPerFrame => maxYawPerFrame;
        public float InputSmoothing => inputSmoothing;
        public float DeadZoneAngle => deadZoneAngle;
        public float CatchUpBand => catchUpBand;
        public float CatchUpSpeed => catchUpSpeed;
        public float MaxCatchUpSpeed => maxCatchUpSpeed;
        public float AdsRigidity => adsRigidity;

        public float MaxReticleOffset => maxReticleOffset;
        public bool ReturnToCenter => returnToCenter;
        public float ReturnSpeed => returnSpeed;

        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;
        public Vector3 AimOriginOffset => aimOriginOffset;
        public float BaseFov => baseFov;
        public float AdsFovMultiplier => adsFovMultiplier;
    }
}