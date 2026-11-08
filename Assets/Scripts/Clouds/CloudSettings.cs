using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lake.Clouds
{
    /// <summary>
    /// Параметры облаков и владелец материалов рейтрейса.
    ///
    /// Заменяет оригинальный CloudGenerator из пакета todoval/CloudsShader.
    /// Тот работал через OnRenderImage и Graphics.Blit — в HDRP эти callbacks
    /// не вызываются вообще, поэтому здесь только данные и материалы, а всю
    /// отрисовку забирает <see cref="CloudRenderFeature"/>.
    ///
    /// Публичные поля и их смысл сохранены один в один, чтобы настройки из
    /// оригинального ассета переносились без переучивания.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Lake/Clouds/Cloud Settings")]
    public sealed class CloudSettings : MonoBehaviour
    {
        [Header("Контейнер")]
        [Tooltip("Куб, внутри которого маршится луч. Задаёт границы слоя.")]
        public Transform container;

        [Header("Освещение")]
        [Tooltip("0 — без света, 1 — свет из параметра сцены, 2 — направленный источник")]
        public int lightingType = 1;

        [Tooltip("Источник света сцены для подсветки облаков")]
        public Light sceneLight;

        [Tooltip("Яркость подсветки облаков при полном солнце. Солнце в полдень даёт «daylightReference», " +
            "к вечеру облака гаснут вместе с ним: раньше подсветка была постоянной и тучи светились " +
            "одинаково днём и в полночь")]
        public float cloudLightIntensity = 2.2f;

        [Tooltip("Интенсивность солнца в полдень — по ней облака понимают, день сейчас или ночь. " +
            "Должна совпадать с кривой Sun Lux в цикле суток (2.4)")]
        public float daylightReference = 2.4f;

        [Tooltip("Цвет облаков, когда солнце село: тучи подсвечивает только небо. Совсем чёрные " +
            "закрывают ночное небо целиком, и синий градиент за ними пропадает")]
        public Color nightCloudColor = new Color(0.06f, 0.07f, 0.10f);

        [Header("Фаза Хенье-Гринштейна")]
        public float henyeyCoeff = 0.75f;
        [Tooltip("Доля прямого рассеяния. 0 — только многократное.")]
        public float henyeyRatio = 0.4f;
        public float henyeyIntensity = 1.0f;

        [Header("Powder (powder effect, Horizon Zero Dawn)")]
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
        [Tooltip("Поглощение. Больше — плотнее и темнее.")]
        public float absorptionCoeff = 0.6f;
        [Tooltip("Размер плитки шума в мирах")]
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
        [Tooltip("Множитель шага за итерацию. 1 — равномерный шаг.")]
        public float rayMarchDecrease = 1.05f;

        [Header("Качество")]
        [Range(0.25f, 1f)]
        [Tooltip("Доля полного разрешения для марча. Ниже — быстрее и мыльнее.")]
        public float marchScale = 0.5f;

        [Header("Текстуры шума")]
        [Tooltip("ShapeNoise — трёхмерный шум формы облаков. Приезжает с пакетом.")]
        public Texture3D shapeNoise;

        [Tooltip("DetailNoise — трёхмерный шум, размывающий края")]
        public Texture3D detailNoise;

        [Tooltip("WeatherMap — покрытие (R), высота (G), плотность (B)")]
        public Texture2D weatherMap;

        [Tooltip("Синий шум для джиттера луча и света")]
        public Texture2D blueNoise;

        [Header("Шейдеры")]
        public Shader renderingShader;
        public Shader blendingShader;

        private Material _renderingMaterial;
        private Material _blendingMaterial;

        /// <summary>Материал рейтрейса. Создаётся лениво, чтобы не трогать ассеты.</summary>
        public Material RenderingMaterial
        {
            get
            {
                if (_renderingMaterial == null)
                {
                    var shader = renderingShader != null ? renderingShader : Shader.Find("CloudRendering");
                    _renderingMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                }
                return _renderingMaterial;
            }
        }

        /// <summary>Материал склейки облаков со сценой.</summary>
        public Material BlendingMaterial
        {
            get
            {
                if (_blendingMaterial == null)
                {
                    var shader = blendingShader != null ? blendingShader : Shader.Find("EnvironmentBlending");
                    _blendingMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                }
                return _blendingMaterial;
            }
        }

        /// <summary>Кто рисует: <see cref="CloudRenderFeature"/> ищет активный экземпляр.</summary>
        public static CloudSettings Instance { get; private set; }

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnDestroy()
        {
            DestroySafely(_renderingMaterial);
            DestroySafely(_blendingMaterial);
        }

        private static void DestroySafely(Object obj)
        {
            if (obj == null)
                return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        /// <summary>
        /// Переносит все настройки и текстуры шума в материал рейтрейса.
        /// Вызывается каждый кадр перед отрисовкой.
        /// </summary>
        public void ApplyToMaterial(Material material)
        {
            if (material == null)
                return;

            material.SetFloat("_henyeyCoeff", henyeyCoeff);
            material.SetFloat("_henyeyRatio", henyeyRatio);
            material.SetFloat("_henyeyIntensity", henyeyIntensity);

            material.SetFloat("_powderCoeff", powderCoeff);
            material.SetFloat("_powderAmount", powderAmount);
            material.SetFloat("_powderIntensity", powderIntensity);

            material.SetFloat("_detailAmount", detailAmount);
            material.SetFloat("_maxDetailModifier", detailModifier);
            material.SetFloat("_densityConstant", densityConstant);
            material.SetFloat("_cloudMaxHeight", cloudMaxHeight);
            material.SetFloat("_cloudHeightModifier", cloudHeightModifier);
            material.SetFloat("_cloudBottomModifier", cloudBottomModifier);

            material.SetFloat("_absorptionCoef", absorptionCoeff);
            material.SetFloat("_tileSize", Mathf.Max(1f, tileSize));
            material.SetFloat("_speed", speed);
            material.SetColor("_cloudColor", color);
            material.SetFloat("_cloudIntensity", cloudIntensity);

            material.SetInt("_useBlueNoiseLight", useBlueNoiseLight ? 1 : 0);
            material.SetInt("_useBlueNoiseRay", useBlueNoiseRay ? 1 : 0);
            material.SetFloat("_blueNoiseLightAmount", blueNoiseLightAmount);
            material.SetFloat("_blueNoiseRayAmount", blueNoiseRayAmount);

            material.SetInt("_lightMarchSteps", Mathf.Clamp(lightMarchSteps, 1, 16));
            material.SetFloat("_lightMarchDecrease", lightMarchDecrease);
            material.SetFloat("_rayMarchStepSize", rayMarchStepSize);
            material.SetFloat("_rayMarchDecrease", rayMarchDecrease);

            // Свет. Для ночной сцены источник ставится далеко по направлению
            // на луну, иначе марш к свету уходит за пределы контейнера.
            bool useLight = lightingType != 0 && sceneLight != null;
            material.SetInt("_useLight", useLight ? 1 : 0);
            if (useLight)
            {
                var pos = sceneLight.transform.position;
                var toLight = sceneLight.transform.forward;
                // Источник выносим далеко по направлению: облака живут внутри
                // контейнера, и реальная позиция лампы внутри него ломает марш.
                material.SetVector("_lightPosition", pos + toLight * 100000f);
                // Яркость идёт от интенсивности светила. Раньше она бралась
                // только из cloudLightIntensity — «интенсивность в люксах
                // бесполезна»; это верно при экспозиции, а сцена теперь живёт
                // без неё, и у солнца 2.4 в полдень против 0 ночью. Иначе
                // тучи светятся белым в полночь и оранжевым на закате.
                float day = daylightReference > 0f
                    ? Mathf.Clamp01(sceneLight.intensity / daylightReference)
                    : 1f;

                Color sunLit = sceneLight.color * cloudLightIntensity;
                material.SetColor("_lightColor", Color.Lerp(nightCloudColor, sunLit, day));
                material.SetFloat("_lightIntensity", 1f);
            }
            else
            {
                material.SetVector("_lightPosition", new Vector3(0f, 1000f, 0f));
                material.SetColor("_lightColor", Color.white);
                material.SetFloat("_lightIntensity", 0f);
            }

            ApplyTextures(material);
            ApplyContainerBounds(material);
        }

        /// <summary>
        /// Текстуры шума. Раньше тянулись через Resources.Load, но он вернул
        /// null на всех четырёх — плотность везде нулевая и облаков нет.
        /// Теперь это прямые ссылки на ассеты: работает и в редакторе, и в сборке,
        /// и не зависит от того, как Unity проиндексировала папку Resources.
        /// </summary>
        private void ApplyTextures(Material material)
        {
            if (shapeNoise != null) material.SetTexture("_ShapeTexture", shapeNoise);
            if (detailNoise != null) material.SetTexture("_DetailTexture", detailNoise);
            if (weatherMap != null) material.SetTexture("_WeatherMap", weatherMap);
            if (blueNoise != null) material.SetTexture("_BlueNoise", blueNoise);
        }

        /// <summary>
        /// Границы AABB берём из куба-контейнера. Без них марш не знает,
        /// где кончается слой, и луч уходит в пустоту.
        /// </summary>
        private void ApplyContainerBounds(Material material)
        {
            if (container == null)
            {
                // Фолбэк: куб 2000 м, слой от 600 до 1600 м над камерой.
                material.SetVector("containerBound_Min", new Vector4(-1000f, 600f, -1000f, 0f));
                material.SetVector("containerBound_Max", new Vector4(1000f, 1600f, 1000f, 0f));
                return;
            }

            // Границы берём из трансформа, а не из MeshFilter: контейнер это
            // пустой объём, меша на нём нет. Раньше код брал bounds меша и
            // падал с MissingComponentException — рейтрейс при этом не
            // рисовался вовсе, потому что исключение роняло весь проход.
            // localScale задаёт ПОЛНЫЙ размер грани, поэтому половина.
            Vector3 half = container.lossyScale * 0.5f;
            material.SetVector("containerBound_Min", container.position - half);
            material.SetVector("containerBound_Max", container.position + half);
        }
    }
}