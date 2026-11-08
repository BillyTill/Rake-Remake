using UnityEngine;

namespace Lake.Bodycam.Config
{
    /// <summary>
    /// Конфиг процедурной анимации оружия (Слой 2 Bodycam).
    /// Объединяет инерцию оружия (PlayerWeapons) и фонарика (Flashlight) + новые каналы sway/bob/breathing/recoil.
    /// Дефолты инерции взяты из PlayerWeapons.cs (строки 465-479) и Flashlight.cs (строки 32-38).
    /// </summary>
    [CreateAssetMenu(fileName = "ProceduralWeaponConfig", menuName = "Lake/Bodycam/Procedural Weapon Config")]
    public class ProceduralWeaponConfig : ScriptableObject
    {
        [Header("Existing Inertia (Перенос в Фазе 3)")]
        [Tooltip("Инерция позиции оружия: метры на м/с линейной скорости. PlayerWeapons: 0.010f (x,y), 0.014f (z). Усреднено.")]
        [SerializeField] private float weaponInertia = 0.012f;

        [Tooltip("Инерция фонарика: метры на м/с. Flashlight.positionLag = 0.016f.")]
        [SerializeField] private float flashlightInertia = 0.016f;

        [Tooltip("Насколько ADS гасит инерцию (0..1). PlayerWeapons: steadiness = 1 - _adsT * 0.85f.")]
        [SerializeField] private float adsStabilization = 0.85f;

        [Header("Sway (New — Фаза 3)")]
        [Tooltip("Амплитуда sway от мыши (метры на градус дельты).")]
        [SerializeField] private float swayAmount = 0.02f;

        [Tooltip("Макс. смещение sway (метры).")]
        [SerializeField] private float swayMaxDistance = 0.05f;

        [Tooltip("Скорость сглаживания sway (1/с).")]
        [SerializeField] private float swaySmooth = 8f;

        [Header("Bob (New — Фаза 3)")]
        [Tooltip("Частота шага (циклов/сек) при ходьбе. PlayerCamera.walkCycleSeconds = 1.2s → 0.83 Гц, но bob — это 2 шага за цикл → ~1.66 Гц. Ставим 2.5 для заметности.")]
        [SerializeField] private float bobFrequency = 2.5f;

        [Tooltip("Амплитуда боба (метры). PlayerCamera.stepDrop = 0.05f (вертикаль), stepLateral = 0.034f (бок). Боб — меньшая амплитуда.")]
        [SerializeField] private float bobAmplitude = 0.015f;

        [Tooltip("Множитель амплитуды на бегу. PlayerCamera.runLateralMultiplier = 1.35f.")]
        [SerializeField] private float bobRunMultiplier = 1.35f;

        [Header("Breathing (New — Фаза 3)")]
        [Tooltip("Амплитуда дыхания (метры). PlayerCamera.breathingAmount = 0.007f.")]
        [SerializeField] private float breathingAmplitude = 0.007f;

        [Tooltip("Частота дыхания (циклов/сек). PlayerCamera.breathingFrequency = 0.85f.")]
        [SerializeField] private float breathingFrequency = 0.85f;

        [Header("Recoil (New — Фаза 3)")]
        [Tooltip("Отдача назад (метры). PlayerWeapons: _kickOffset.z -= def.kickBackM (Rifle kickBackM = 0.12f).")]
        [SerializeField] private float recoilKickback = 0.12f;

        [Tooltip("Отдача поворот (градусы). PlayerWeapons: _kickRotation.x -= def.viewKickDeg (Rifle viewKickDeg = 2.5f).")]
        [SerializeField] private float recoilRotation = 2.5f;

        [Tooltip("Скорость восстановления отдачи (1/с). PlayerWeapons decay = 1 - Exp(-9*dt) ≈ 9.")]
        [SerializeField] private float recoilRecoverySpeed = 9f;

        [Header("Weapon Collision (Слой 4, Фаза 4)")]
        [Tooltip("Радиус сферы пробирания ствола.")]
        [SerializeField] private float castRadius = 0.08f;

        [Tooltip("Дальность каста вперёд от дула.")]
        [SerializeField] private float castDistance = 0.6f;

        [Tooltip("Скорость возврата ствола после коллизии (1/с).")]
        [SerializeField] private float pushBackSpeed = 10f;

        [Tooltip("LayerMask для коллизии оружия.")]
        [SerializeField] private LayerMask collisionMask = ~0;

        // Public getters
        public float WeaponInertia => weaponInertia;
        public float FlashlightInertia => flashlightInertia;
        public float AdsStabilization => adsStabilization;

        public float SwayAmount => swayAmount;
        public float SwayMaxDistance => swayMaxDistance;
        public float SwaySmooth => swaySmooth;

        public float BobFrequency => bobFrequency;
        public float BobAmplitude => bobAmplitude;
        public float BobRunMultiplier => bobRunMultiplier;

        public float BreathingAmplitude => breathingAmplitude;
        public float BreathingFrequency => breathingFrequency;

        public float RecoilKickback => recoilKickback;
        public float RecoilRotation => recoilRotation;
        public float RecoilRecoverySpeed => recoilRecoverySpeed;

        public float CastRadius => castRadius;
        public float CastDistance => castDistance;
        public float PushBackSpeed => pushBackSpeed;
        public LayerMask CollisionMask => collisionMask;
    }
}