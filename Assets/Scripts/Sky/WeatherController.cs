using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Lake.Sky
{
    /// <summary>
    /// Погодные условия, сведённые к одному набору чисел.
    ///
    /// Это «шина» между пресетами погоды и тем, кто их применяет
    /// (<see cref="DayNightCycle"/>): облака, туман, ветер и затемнение света.
    /// Отдельным типом, а не ссылкой на пресет, потому что во время перехода
    /// значения складываются из двух пресетов сразу.
    /// </summary>
    [Serializable]
    public struct WeatherState
    {
        [Tooltip("Форма облаков: пресет встроенных объёмных облаков HDRP")]
        public VolumetricClouds.CloudPresets preset;

        [Tooltip("Плотность облачного слоя (HDRP densityMultiplier)")]
        public float density;

        [Tooltip("Нижняя граница облачного слоя, метры")]
        public float bottomAltitude;

        [Tooltip("Толщина облачного слоя, метры")]
        public float altitudeRange;

        [Tooltip("Оценка доли закрытого неба, 0…1")]
        public float coverage;

        [Tooltip("Оттенок рассеяния в облаке")]
        public Color scatteringTint;

        [Tooltip("Доля небесного света, доходящая до облаков (HDRP ambientLightProbeDimmer)")]
        public float ambientDimmer;

        [Tooltip("Доля прямого света, доходящая до облаков (HDRP sunLightDimmer)")]
        public float sunDimmer;

        [Tooltip("Множитель к суточной дальности видимости в тумане. 1 — как задано циклом, 0.5 — вдвое плотнее")]
        public float fogScale;

        [Tooltip("Дополнительный оттенок тумана")]
        public Color fogTint;

        [Tooltip("Скорость ветра, км/ч")]
        public float windSpeedKmh;

        [Tooltip("Куда дует ветер, градусы")]
        public float windOrientation;

        [Tooltip("Во что облака окрашивают небо (подменяют градиент)")]
        public Color skyTint;

        [Tooltip("Насколько сильно облака подменяют небо: 0 — небо как есть, 1 — целиком цвет облаков")]
        public float skyBlend;

        /// <summary>Оценка доли закрытого неба, 0…1.</summary>
        public float Coverage => Mathf.Clamp01(coverage);

        /// <summary>Промежуточное состояние между двумя пресетами.</summary>
        public static WeatherState Lerp(WeatherState a, WeatherState b, float t)
        {
            return new WeatherState
            {
                // Форму облаков не размазать: пресет переключается целиком,
                // в середине перехода. Плотность и ярус при этом едут плавно,
                // поэтому смена читается как смена погоды, а не как рывок.
                preset = t < 0.5f ? a.preset : b.preset,
                density = Mathf.Lerp(a.density, b.density, t),
                bottomAltitude = Mathf.Lerp(a.bottomAltitude, b.bottomAltitude, t),
                altitudeRange = Mathf.Lerp(a.altitudeRange, b.altitudeRange, t),
                coverage = Mathf.Lerp(a.coverage, b.coverage, t),
                scatteringTint = Color.Lerp(a.scatteringTint, b.scatteringTint, t),
                ambientDimmer = Mathf.Lerp(a.ambientDimmer, b.ambientDimmer, t),
                sunDimmer = Mathf.Lerp(a.sunDimmer, b.sunDimmer, t),
                fogScale = Mathf.Lerp(a.fogScale, b.fogScale, t),
                fogTint = Color.Lerp(a.fogTint, b.fogTint, t),
                windSpeedKmh = Mathf.Lerp(a.windSpeedKmh, b.windSpeedKmh, t),
                windOrientation = Mathf.LerpAngle(a.windOrientation, b.windOrientation, t),
                skyTint = Color.Lerp(a.skyTint, b.skyTint, t),
                skyBlend = Mathf.Lerp(a.skyBlend, b.skyBlend, t),
            };
        }
    }

    /// <summary>
    /// Пресет погоды: полный набор чисел, который применяется целиком.
    /// Все поля открыты в инспекторе — это и есть «полная настройка погоды».
    ///
    /// Почему форма облаков задана пресетом HDRP, а не своими множителями ярусов.
    /// В простом режиме (<c>CloudControl.Simple</c>) HDRP берёт готовую карту
    /// покрытия, а расширенный и ручной режимы сами пекут её из текстур
    /// cumulus/altostratus/cumulonimbus — в этом профиле они пусты, поэтому
    /// расширенный режим дал бы небо вообще без облаков. Отсюда выбор: пресеты
    /// плюс плотность, ярус и ветер, которые цикл суток ведёт плавно.
    /// </summary>
    [Serializable]
    public sealed class WeatherPreset
    {
        [Tooltip("Имя для консоли и для переключения по имени")]
        public string name = "Ясно";

        [Header("Облака")]
        [Tooltip("Форма и кривые облаков: Sparse — редкие, Cloudy — средние, Overcast — сплошная пелена, Stormy — тяжёлые тучи")]
        public VolumetricClouds.CloudPresets preset = VolumetricClouds.CloudPresets.Cloudy;

        [Tooltip("Плотность слоя: 0.2 — редкие клочья, 0.4 — плотная пелена, 0.6 — чернота")]
        [Range(0f, 0.9f)] public float density = 0.3f;

        [Min(10f)] public float bottomAltitude = 2000f;

        [Min(100f)] public float altitudeRange = 2000f;

        [Tooltip("Оценка доли закрытого неба, 0…1: гасит светила и подменяет небо облачным свечением")]
        [Range(0f, 1f)] public float coverage = 0.35f;

        [ColorUsage(false)] public Color scatteringTint = new Color(0.72f, 0.80f, 1f);

        [Range(0f, 1f)] public float ambientDimmer = 0.7f;

        [Range(0f, 1f)] public float sunDimmer = 1f;

        [Header("Небо под облаками")]
        [ColorUsage(false)] public Color skyTint = new Color(0.55f, 0.60f, 0.72f);

        [Range(0f, 1f)] public float skyBlend = 0.25f;

        [Header("Туман")]
        [Range(0.05f, 3f)] public float fogScale = 1f;

        [ColorUsage(false)] public Color fogTint = Color.white;

        [Header("Ветер")]
        [Range(0f, 200f)] public float windSpeedKmh = 20f;

        [Range(0f, 360f)] public float windOrientation = 30f;

        /// <summary>Снимок пресета как значения погоды.</summary>
        public WeatherState ToState()
        {
            return new WeatherState
            {
                preset = preset,
                density = density,
                bottomAltitude = bottomAltitude,
                altitudeRange = altitudeRange,
                coverage = coverage,
                scatteringTint = scatteringTint,
                ambientDimmer = ambientDimmer,
                sunDimmer = sunDimmer,
                fogScale = fogScale,
                fogTint = fogTint,
                windSpeedKmh = windSpeedKmh,
                windOrientation = windOrientation,
                skyTint = skyTint,
                skyBlend = skyBlend,
            };
        }

        /// <summary>
        /// Набор по умолчанию: пять состояний от чистого неба до шторма и тумана.
        /// Порядок важен — по нему идёт и автосмена, и перебор по индексу.
        /// </summary>
        public static List<WeatherPreset> DefaultPresets()
        {
            return new List<WeatherPreset>
            {
                new WeatherPreset
                {
                    name = "Ясно",
                    preset = VolumetricClouds.CloudPresets.Sparse,
                    density = 0.20f, bottomAltitude = 3000f, altitudeRange = 1000f,
                    coverage = 0.20f,
                    scatteringTint = new Color(0.78f, 0.85f, 1f),
                    ambientDimmer = 0.75f, sunDimmer = 1f,
                    skyTint = new Color(0.55f, 0.65f, 0.85f), skyBlend = 0.15f,
                    fogScale = 1.15f, fogTint = Color.white,
                    windSpeedKmh = 12f, windOrientation = 40f,
                },
                new WeatherPreset
                {
                    name = "Переменно",
                    preset = VolumetricClouds.CloudPresets.Cloudy,
                    density = 0.30f, bottomAltitude = 2200f, altitudeRange = 1600f,
                    coverage = 0.45f,
                    scatteringTint = new Color(0.74f, 0.82f, 1f),
                    ambientDimmer = 0.70f, sunDimmer = 0.85f,
                    skyTint = new Color(0.50f, 0.56f, 0.70f), skyBlend = 0.35f,
                    fogScale = 1f, fogTint = Color.white,
                    windSpeedKmh = 22f, windOrientation = 60f,
                },
                new WeatherPreset
                {
                    name = "Пасмурно",
                    preset = VolumetricClouds.CloudPresets.Overcast,
                    density = 0.40f, bottomAltitude = 1500f, altitudeRange = 2500f,
                    coverage = 0.85f,
                    scatteringTint = new Color(0.70f, 0.75f, 0.90f),
                    ambientDimmer = 0.60f, sunDimmer = 0.45f,
                    skyTint = new Color(0.42f, 0.46f, 0.56f), skyBlend = 0.75f,
                    fogScale = 0.80f, fogTint = new Color(0.92f, 0.94f, 1f),
                    windSpeedKmh = 35f, windOrientation = 90f,
                },
                new WeatherPreset
                {
                    name = "Шторм",
                    preset = VolumetricClouds.CloudPresets.Stormy,
                    density = 0.50f, bottomAltitude = 1000f, altitudeRange = 5000f,
                    coverage = 1f,
                    scatteringTint = new Color(0.55f, 0.60f, 0.72f),
                    ambientDimmer = 0.45f, sunDimmer = 0.20f,
                    skyTint = new Color(0.24f, 0.26f, 0.34f), skyBlend = 0.90f,
                    fogScale = 0.60f, fogTint = new Color(0.80f, 0.83f, 0.92f),
                    windSpeedKmh = 75f, windOrientation = 140f,
                },
                new WeatherPreset
                {
                    name = "Туман",
                    preset = VolumetricClouds.CloudPresets.Cloudy,
                    density = 0.34f, bottomAltitude = 900f, altitudeRange = 1500f,
                    coverage = 0.70f,
                    scatteringTint = new Color(0.72f, 0.78f, 0.92f),
                    ambientDimmer = 0.55f, sunDimmer = 0.35f,
                    skyTint = new Color(0.35f, 0.38f, 0.48f), skyBlend = 0.80f,
                    fogScale = 0.28f, fogTint = new Color(0.85f, 0.88f, 0.95f),
                    windSpeedKmh = 5f, windOrientation = 20f,
                },
            };
        }
    }

    /// <summary>
    /// Погода: текущий пресет, плавные переходы и автосмена.
    ///
    /// Сама ничего не применяет — только считает <see cref="State"/>, который
    /// каждый кадр читает <see cref="DayNightCycle"/>. Один писатель на объём:
    /// два компонента, тянущих одни и те же ручки, дерутся за кадр.
    ///
    /// Время приходит снаружи, в игровых минутах: сутки и погода должны жить
    /// на одних часах, иначе при ускорении времени погода отстанет.
    /// </summary>
    [AddComponentMenu("Lake/Sky/Weather Controller")]
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class WeatherController : MonoBehaviour
    {
        [Header("Пресеты")]
        [Tooltip("Полный набор состояний. Порядок = порядок обхода в автосмене")]
        public List<WeatherPreset> presets = WeatherPreset.DefaultPresets();

        [Tooltip("С какого пресета начинать, индекс в списке")]
        public int startIndex = 2;

        [Header("Переходы")]
        [Tooltip("Сколько игровых минут длится смена погоды. 0 — менять мгновенно")]
        public float transitionMinutes = 45f;

        [Header("Автосмена")]
        [Tooltip("Менять погоду самостоятельно")]
        public bool auto = true;

        [Tooltip("Нижняя граница интервала автосмены, игровые минуты")]
        public float autoMinMinutes = 150f;

        [Tooltip("Верхняя граница интервала автосмены, игровые минуты")]
        public float autoMaxMinutes = 420f;

        [Header("Отладка")]
        [Tooltip("Писать в консоль каждую смену погоды")]
        public bool logChanges = true;

        /// <summary>Погода сменилась: имя нового пресета.</summary>
        public event Action<string> WeatherChanged;

        /// <summary>Текущие значения. Обновляются в <see cref="Tick"/>.</summary>
        public WeatherState State { get; private set; }

        public int CurrentIndex { get; private set; }

        public string CurrentName =>
            presets != null && CurrentIndex >= 0 && CurrentIndex < presets.Count
                ? presets[CurrentIndex].name
                : "—";

        /// <summary>0 — переход только начался, 1 — переход завершён.</summary>
        public float Blend { get; private set; } = 1f;

        public bool IsTransitioning => Blend < 1f;

        /// <summary>Сколько игровых минут осталось до следующей автосмены.</summary>
        public float MinutesUntilNextWeather => _untilNext;

        /// <summary>
        /// Погода для показа в редакторе: контроллер там не просыпается, поэтому
        /// берём стартовый пресет как есть. Игра берёт <see cref="State"/>.
        /// </summary>
        public WeatherState PreviewState
        {
            get
            {
                EnsurePresets();
                return presets[Mathf.Clamp(startIndex, 0, presets.Count - 1)].ToState();
            }
        }

        private WeatherState _from;
        private WeatherState _to;
        private float _untilNext;

        private void Awake()
        {
            EnsurePresets();
            SwitchTo(Mathf.Clamp(startIndex, 0, presets.Count - 1), instant: true);
            PlanNext();
        }

        private void OnValidate()
        {
            EnsurePresets();

            transitionMinutes = Mathf.Max(0f, transitionMinutes);
            autoMinMinutes = Mathf.Max(1f, autoMinMinutes);
            autoMaxMinutes = Mathf.Max(autoMinMinutes, autoMaxMinutes);
            startIndex = presets.Count == 0 ? 0 : Mathf.Clamp(startIndex, 0, presets.Count - 1);

            // Правка пресета в инспекторе должна быть видна на сцене сразу:
            // цикл суток рисует то, что здесь написано.
            if (!Application.isPlaying)
            {
                var cycle = UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();

                if (cycle != null)
                    cycle.RequestApply();
            }
        }

        /// <summary>Игровой день живёт на часах цикла суток — он и дёргает погоду.</summary>
        public void Tick(float gameMinutes)
        {
            if (gameMinutes > 0f)
            {
                if (Blend < 1f)
                {
                    Blend = transitionMinutes > 0.001f
                        ? Mathf.Min(1f, Blend + gameMinutes / transitionMinutes)
                        : 1f;
                }

                if (auto && !IsTransitioning)
                {
                    _untilNext -= gameMinutes;
                    if (_untilNext <= 0f)
                        SetRandomWeather();
                }
            }

            // Сглаживание по краям: линейное начало перехода читается рывком
            // плотности — глаз цепляется за мгновенную смену яркости неба.
            float k = Blend >= 1f ? 1f : Mathf.SmoothStep(0f, 1f, Blend);
            State = WeatherState.Lerp(_from, _to, k);
        }

        /// <summary>Мгновенно или плавно поставить погоду по индексу.</summary>
        public void SetWeather(int index, bool instant = false)
        {
            if (presets == null || presets.Count == 0)
                return;

            SwitchTo(Mathf.Clamp(index, 0, presets.Count - 1), instant);

            if (auto)
                PlanNext();
        }

        /// <summary>То же по имени пресета. Возвращает false, если имени нет.</summary>
        public bool SetWeather(string presetName, bool instant = false)
        {
            if (presets == null)
                return false;

            for (int i = 0; i < presets.Count; i++)
            {
                if (string.Equals(presets[i].name, presetName, StringComparison.OrdinalIgnoreCase))
                {
                    SetWeather(i, instant);
                    return true;
                }
            }

            return false;
        }

        /// <summary>Случайная погода, кроме текущей: подряд одна и та же — не смена.</summary>
        public void SetRandomWeather(bool instant = false)
        {
            if (presets == null || presets.Count == 0)
                return;

            int index = CurrentIndex;
            if (presets.Count > 1)
            {
                while (index == CurrentIndex)
                    index = UnityEngine.Random.Range(0, presets.Count);
            }

            SetWeather(index, instant);
        }

        private void SwitchTo(int index, bool instant)
        {
            CurrentIndex = index;
            _from = State;
            _to = presets[index].ToState();

            // Первая смена (из Awake) — это инициализация: предыдущего значения нет.
            if (instant || transitionMinutes <= 0.001f)
            {
                _from = _to;
                Blend = 1f;
            }
            else
            {
                Blend = 0f;
            }

            State = _to;

            WeatherChanged?.Invoke(presets[index].name);

            if (logChanges)
                Debug.Log($"[Lake] Погода: {presets[index].name}" +
                          (instant ? string.Empty : $" (переход {transitionMinutes:0} игр. мин)"));
        }

        private void PlanNext()
        {
            _untilNext = UnityEngine.Random.Range(autoMinMinutes, Mathf.Max(autoMinMinutes, autoMaxMinutes));
        }

        private void EnsurePresets()
        {
            if (presets == null || presets.Count == 0)
            {
                presets = WeatherPreset.DefaultPresets();
                Debug.LogWarning("[Lake] Список пресетов погоды был пуст — подставлен набор по умолчанию.");
            }
        }
    }
}
