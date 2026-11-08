using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Lake.Clouds
{
    /// <summary>
    /// Обёртка над облаками из пакета todoval/CloudsShader.
    ///
    /// Оригинал пакета рисовал облака через OnRenderImage и Graphics.Blit.
    /// В HDRP 17 оба обратных вызова не вызываются вообще: Unity их не
    /// вызывает, а типы ScriptableRenderPass и RenderPipelineFeature, которыми
    /// их обычно заменяют, удалены вместе с HDAdditionalRendererData.
    ///
    /// Реестр кастом-проходов в HDRP 17 — компонент CustomPassVolume:
    /// он держит список пассов и точку внедрения, а пассы находятся поиском
    /// по всем томам сцены. Поэтому рисование ушло в CloudCustomPass,
    /// а этот класс занимается данными и регистрацией.
    ///
    /// Публичные поля и их смысл сохранены один в один: значения, выставленные
    /// в инспекторе до правки, продолжают работать без переучивания.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Lake/Clouds/Cloud Generator")]
    public sealed class CloudGenerator : MonoBehaviour
    {
        [Header("Освещение")]
        public int lightingType = 1;
        public Light sceneLight;
        public float cloudLightIntensity = 2.2f;

        [Header("Фаза Хенье-Гринштейна")]
        public float henyeyCoeff = 0.75f;
        public float henyeyRatio = 0.4f;
        public float henyeyIntensity = 1.0f;

        [Header("Powder")]
        public float powderCoeff = 0.5f;
        public float powderAmount = 0.5f;
        public float powderIntensity = 0.5f;

        [Header("Вид")]
        public float cloudIntensity = 1.0f;
        public Color color = Color.white;

        [Header("Форма")]
        public float detailAmount = 0.45f;
        public float detailModifier = 1.5f;
        public float densityConstant = 0.9f;
        public float cloudMaxHeight = 0.4f;
        public float cloudHeightModifier = 0.5f;
        public float cloudBottomModifier = 0.2f;

        [Header("Погода")]
        public float absorptionCoeff = 0.6f;
        public int tileSize = 800;
        public float speed = 1.0f;

        [Header("Синий шум")]
        public bool useBlueNoiseLight = true;
        public bool useBlueNoiseRay = true;
        public float blueNoiseLightAmount = 0.1f;
        public float blueNoiseRayAmount = 0.5f;

        [Header("Марш")]
        public int lightMarchSteps = 6;
        public float lightMarchDecrease = 0.5f;
        public float rayMarchStepSize = 20f;
        public float rayMarchDecrease = 1.05f;

        [Header("Контейнер")]
        [Tooltip("Объём, в котором маршится луч. Меш на нём не рисуется — " +
            "нужны только размеры и положение, они задают границы AABB.")]
        public Transform container;

        [Header("Качество")]
        [Range(0.25f, 1f)]
        public float marchScale = 0.5f;

        private CloudSettings _settings;
        private CloudCustomPass _pass;
        private bool _registered;

        /// <summary>Настройки, в которые зеркалятся поля выше.</summary>
        public CloudSettings Settings => _settings;

        private void OnEnable()
        {
            EnsureSettings();
            ApplyFields();
            EnsureRegistered();
        }

        private void Start()
        {
            // В Play Mode компонент может быть создан динамически после
            // OnEnable, а в edit-режиме OnEnable у MonoBehaviour без
            // [ExecuteAlways] вообще не вызывается. Start — надёжная точка.
            EnsureSettings();
            ApplyFields();
            EnsureRegistered();
        }

        /// <summary>
        /// Создаёт настройки и ставит пасс в том. Вызывается автоматически,
        /// но метод публичный, чтобы регистрацию можно было выполнить вручную
        /// из редактора — OnEnable в edit-режиме не срабатывает.
        /// </summary>
        public void EnsureRegistered()
        {
            EnsureSettings();
            if (!_registered)
            {
                ApplyFields();
                RegisterPass();
            }
        }

        private void LateUpdate()
        {
            // Поля могут быть изменены из инспектора во время игры
            if (_settings != null)
                ApplyFields();

            // Контейнер едет вместе с игроком. Луч маршится внутри AABB,
            // и если контейнер остаётся в точке спавна, то через несколько
            // сотен метров игрок выходит из объёма и облака пропадают.
            var cam = Camera.main;
            if (cam != null && container != null)
                container.position = cam.transform.position;
        }

        private void OnDisable()
        {
            UnregisterPass();
        }

        private void EnsureSettings()
        {
            if (_settings == null)
            {
                _settings = GetComponent<CloudSettings>();
                if (_settings == null)
                    _settings = gameObject.AddComponent<CloudSettings>();
            }
        }

        /// <summary>
        /// Контейнер задаёт только границы объёма. Меш и рендерер не нужны:
        /// луч строится в шейдере из матриц камеры, и объём берётся из AABB.
        /// </summary>
        private void EnsureContainer()
        {
            if (container != null)
                return;

            var existing = GameObject.Find("CloudContainer");
            if (existing != null)
            {
                container = existing.transform;
                return;
            }

            var go = new GameObject("CloudContainer");
            // Камера должна быть внутри объёма: луч стартует в точке входа в AABB,
            // и если камера снаружи, слои над головой в марш не попадут.
            go.transform.localScale = new Vector3(4000f, 2000f, 4000f);
            container = go.transform;
        }

        private void ApplyFields()
        {
            EnsureContainer();

            _settings.lightingType = lightingType;
            _settings.sceneLight = sceneLight;
            _settings.cloudLightIntensity = cloudLightIntensity;

            _settings.henyeyCoeff = henyeyCoeff;
            _settings.henyeyRatio = henyeyRatio;
            _settings.henyeyIntensity = henyeyIntensity;

            _settings.powderCoeff = powderCoeff;
            _settings.powderAmount = powderAmount;
            _settings.powderIntensity = powderIntensity;

            _settings.cloudIntensity = cloudIntensity;
            _settings.color = color;

            _settings.detailAmount = detailAmount;
            _settings.detailModifier = detailModifier;
            _settings.densityConstant = densityConstant;
            _settings.cloudMaxHeight = cloudMaxHeight;
            _settings.cloudHeightModifier = cloudHeightModifier;
            _settings.cloudBottomModifier = cloudBottomModifier;

            _settings.absorptionCoeff = absorptionCoeff;
            _settings.tileSize = tileSize;
            _settings.speed = speed;

            _settings.useBlueNoiseLight = useBlueNoiseLight;
            _settings.useBlueNoiseRay = useBlueNoiseRay;
            _settings.blueNoiseLightAmount = blueNoiseLightAmount;
            _settings.blueNoiseRayAmount = blueNoiseRayAmount;

            _settings.lightMarchSteps = lightMarchSteps;
            _settings.lightMarchDecrease = lightMarchDecrease;
            _settings.rayMarchStepSize = rayMarchStepSize;
            _settings.rayMarchDecrease = rayMarchDecrease;

            _settings.container = container;
            _settings.marchScale = marchScale;

            // Шейдеры берём из пакета. Имена оригинальные:
            // "CloudRendering" и "EnvironmentBlending".
            if (_settings.renderingShader == null)
                _settings.renderingShader = Shader.Find("CloudRendering");
            if (_settings.blendingShader == null)
                _settings.blendingShader = Shader.Find("EnvironmentBlending");
        }

        private void RegisterPass()
        {
            if (_registered)
                return;

            var volume = GetComponent<CustomPassVolume>();
            if (volume == null)
                volume = gameObject.AddComponent<CustomPassVolume>();

            volume.isGlobal = true;
            _pass = CloudCustomPass.Install(volume, _settings, marchScale);
            _registered = _pass != null;
        }

        private void UnregisterPass()
        {
            var volume = GetComponent<CustomPassVolume>();
            if (volume == null)
                return;

            for (int i = volume.customPasses.Count - 1; i >= 0; i--)
                if (volume.customPasses[i] is CloudCustomPass)
                    volume.customPasses.RemoveAt(i);

            _pass = null;
            _registered = false;
        }
    }
}