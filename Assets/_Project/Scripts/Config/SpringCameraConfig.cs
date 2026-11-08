using UnityEngine;

namespace Lake.Bodycam.Config
{
    /// <summary>
    /// Конфиг пружинной камеры (Слой 1 Bodycam) + перенос существующей инерции PlayerCamera.
    /// Дефолты инерции взяты из PlayerCamera.cs (строки 32-42).
    /// </summary>
    [CreateAssetMenu(fileName = "SpringCameraConfig", menuName = "Lake/Bodycam/Spring Camera Config")]
    public class SpringCameraConfig : ScriptableObject
    {
        [Header("Spring (New — Слой 1 Bodycam) — PHASE 4: not used in Phase 2")]
        [Tooltip("PHASE 4: Жёсткость пружины углов. Не используется в Фазе 2.")]
        [SerializeField] private float stiffness = 120f;

        [Tooltip("PHASE 4: Затухание пружины углов. Не используется в Фазе 2.")]
        [SerializeField] private float damping = 20f;

        [Tooltip("PHASE 4: Макс. угловая скорость пружины. Не используется в Фазе 2.")]
        [SerializeField] private float maxAngularVelocity = 1080f;

        [Tooltip("PHASE 4: Позиционное смещение камеры от AimSource. Не используется в Фазе 2.")]
        [SerializeField] private Vector3 positionOffset = new Vector3(0.2f, 0.1f, -0.1f);

        [Tooltip("PHASE 4: Поворотное смещение камеры от AimSource. Не используется в Фазе 2.")]
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

        [Header("Existing PlayerCamera Inertia (Перенос в Фазе 2)")]
        [Tooltip("Позиционная инерция: метры на градус/сек. PlayerCamera.positionalSway = 0.0005f.")]
        [SerializeField] private float positionalSway = 0.0005f;

        [Tooltip("Потолок позиционного смещения. PlayerCamera.maxPositionalSway = 0.014f.")]
        [SerializeField] private float maxPositionalSway = 0.014f;

        [Tooltip("Скорость возврата позиционной инерции (1/с). PlayerCamera.swayReturn = 17f.")]
        [SerializeField] private float swayReturn = 17f;

        [Tooltip("Ротационная инерция: градусы на градус/сек. PlayerCamera.rotationalSwayPerDegree = 0.035f.")]
        [SerializeField] private float rotationalSwayPerDegree = 0.035f;

        [Tooltip("Потолок ротационного смещения. PlayerCamera.maxRotationalSway = 1.0f.")]
        [SerializeField] private float maxRotationalSway = 1.0f;

        [Tooltip("Скорость возврата ротационной инерции (1/с). PlayerCamera.rotationalSwayReturn = 15f.")]
        [SerializeField] private float rotationalSwayReturn = 15f;

        [Tooltip("SmoothTime для Vector2.SmoothDamp угловой скорости взгляда. PlayerCamera строка с 0.04f.")]
        [SerializeField] private float aimAngularSmoothTime = 0.04f;

        [Tooltip("Скорость догона lean при следовании за прицелом. PlayerCamera строка с dt * 6f.")]
        [SerializeField] private float catchUpLeanRate = 6f;

        // PHASE 4: not used in Phase 2 — задел под будущую пружину
        [Tooltip("PHASE 4: Скорость догона прицела. Не используется в Фазе 2.")]
        [SerializeField] private float catchUpRate = 6f;

        [Tooltip("PHASE 4: Макс. скорость догона. Не используется в Фазе 2.")]
        [SerializeField] private float maxCatchUpSpeed = 220f;

        [Header("Collision (Слой 4, Фаза 4) — PHASE 4: not used in Phase 2")]
        [Tooltip("PHASE 4: Радиус сферы пробирания камеры. Не используется в Фазе 2.")]
        [SerializeField] private float probeRadius = 0.2f;

        [Tooltip("PHASE 4: Мин. дистанция до камеры при коллизии. Не используется в Фазе 2.")]
        [SerializeField] private float minDistance = 0.3f;

        [Tooltip("PHASE 4: LayerMask для коллизии камеры. Не используется в Фазе 2.")]
        [SerializeField] private LayerMask collisionMask = ~0;

        // Public getters
        public float Stiffness => stiffness;
        public float Damping => damping;
        public float MaxAngularVelocity => maxAngularVelocity;
        public Vector3 PositionOffset => positionOffset;
        public Vector3 RotationOffset => rotationOffset;

        // PlayerCamera inertia (Phase 2)
        public float PositionalSway => positionalSway;
        public float MaxPositionalSway => maxPositionalSway;
        public float SwayReturn => swayReturn;
        public float RotationalSwayPerDegree => rotationalSwayPerDegree;
        public float MaxRotationalSway => maxRotationalSway;
        public float RotationalSwayReturn => rotationalSwayReturn;
        public float AimAngularSmoothTime => aimAngularSmoothTime;
        public float CatchUpLeanRate => catchUpLeanRate;
        public float CatchUpRate => catchUpRate;
        public float MaxCatchUpSpeed => maxCatchUpSpeed;

        public float ProbeRadius => probeRadius;
        public float MinDistance => minDistance;
        public LayerMask CollisionMask => collisionMask;
    }
}