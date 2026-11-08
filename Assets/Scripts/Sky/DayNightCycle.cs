using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;

namespace Lake.Sky
{
    /// <summary>
    /// Полный цикл суток: время, солнце и луна, небо, туман, облака, экспозиция.
    ///
    /// Почему кривые, а не физика. Экспозиция сцены зафиксирована (EV100 = 10) —
    /// это осознанное решение ради ночного хоррора: фонарик на 181 000 кд светит
    /// «правильно» только при ней. Но тогда ни солнце, ни луна в реальных
    /// величинах не работают: настоящие 100 000 лк выжигают кадр, а 0,3 лк
    /// луны не видно вообще. Поэтому светила, небо и туман заданы кривыми по
    /// часам, а не ваттами. Смысл величин подписан у каждого поля.
    ///
    /// Всё пишется в <b>экземпляр</b> профиля (<c>Volume.profile</c>), поэтому
    /// ассет при запуске не портится: значения живут до выхода из Play Mode.
    /// В редакторе, наоборот, правки идут прямо в ассет — это то, что даёт
    /// предпросмотр времени на ползунке без запуска игры.
    ///
    /// Экспозиция сцены не ведётся: компонент Exposure в профиле выключен,
    /// поэтому кадр показывает яркость как есть — 1.0 это белый. Солнце, луна,
    /// небо и фонарик записаны в этих же единицах. Включите Exposure обратно —
    /// и всё станет в 2048 раз темнее (2^11): либо держите в нём fixedExposure
    /// около нуля, либо умножьте кривые на 2048.
    ///
    /// Кадры для проверки снимает <see cref="Lake.Tools.SceneShots"/>.
    /// </summary>
    [AddComponentMenu("Lake/Sky/Day Night Cycle")]
    [DisallowMultipleComponent]
    public sealed class DayNightCycle : MonoBehaviour
    {
        // ── Время ───────────────────────────────────────────────────────────

        [Header("Время")]
        [Tooltip("Текущий час суток: 0 — полночь, 6 — восход, 12 — полдень, 18 — закат. " +
                 "Ползунок двигается и в игре, и в редакторе — сцена меняется сразу")]
        [FormerlySerializedAs("startHour")]
        [Range(0f, 24f)] public float hour = 0.5f;

        [Tooltip("Менять сцену прямо в редакторе, без Play Mode. Правки уходят в ассет профиля " +
                 "('Sky and Fog Settings') — это то, что даёт предпросмотр времени на ползунке")]
        public bool previewInEditor = true;

        [Tooltip("Сколько реальных минут длится игровой день. 24 — сутки за 24 минуты")]
        [Min(0.1f)] public float dayLengthMinutes = 24f;

        [Tooltip("Множитель скорости времени. 0 — время стоит")]
        [Min(0f)] public float timeScale = 1f;

        [Tooltip("Пауза: время не идёт, погода не меняется. Сцена остаётся освещённой как есть")]
        public bool paused;

        // ── Ссылки ──────────────────────────────────────────────────────────

        [Header("Ссылки")]
        [Tooltip("Directional Light солнца")]
        public Light sun;

        [Tooltip("Directional Light луны. Едет в противофазе с солнцем")]
        public Light moon;

        [Tooltip("Volume с профилем неба и тумана ('Sky and Fog Volume')")]
        public Volume volume;

        [Tooltip("Погода. Не задана — берётся спокойный набор облаков по умолчанию")]
        public WeatherController weather;

        // ── Светила ─────────────────────────────────────────────────────────

        [Header("Светила")]
        [Tooltip("Азимут плоскости, по которой ходят солнце и луна, градусы")]
        [Range(0f, 360f)] public float pathAzimuth = 20f;

        [Tooltip("Цвет солнца по часам (доля суток: 0.25 = 6:00)")]
        [GradientUsage(true)] public Gradient sunColor = BuildSunColor();

        [Tooltip("Освещённость солнца по часам. Экспозиция не ведётся, поэтому значения в единицах кадра: " +
                 "полдень 5 — у земли с альбедо 0.18 это светлый день, 12 и выше — уже белое")]
        public AnimationCurve sunLux = BuildSunLux();

        [Tooltip("Цвет луны по часам")]
        [GradientUsage(true)] public Gradient moonColor = BuildMoonColor();

        [Tooltip("Освещённость луны по часам. Луна — заполнитель, а не свет: ночь держат фонарик и небо, " +
                 "настоящие 0.3 лк в этих единицах не видно вообще")]
        public AnimationCurve moonLux = BuildMoonLux();

        // ── Небо ────────────────────────────────────────────────────────────

        [Header("Небо")]
        [Tooltip("Зенит по часам. Цвета линейные, без гаммы")]
        [GradientUsage(true)] public Gradient skyTop = BuildSkyTop();

        [Tooltip("Середина неба по часам")]
        [GradientUsage(true)] public Gradient skyMiddle = BuildSkyMiddle();

        [Tooltip("Горизонт по часам")]
        [GradientUsage(true)] public Gradient skyBottom = BuildSkyBottom();

        [Tooltip("Яркость неба по часам — множитель HDRP. Ночью цвета тёмные и множитель большой (0.83), " +
                 "днём цвета светлые и множитель малый (0.55): иначе небо уходит в белую засветку")]
        public AnimationCurve skyMultiplier = BuildSkyMultiplier();

        [Tooltip("Размытие градиента неба (HDRP gradientDiffusion)")]
        [Range(0f, 10f)] public float skyDiffusion = 1f;

        // ── Туман ───────────────────────────────────────────────────────────

        [Header("Туман")]
        [Tooltip("Дальность видимости в тумане по часам, метры. Меньше — плотнее. Погода умножает это значение")]
        public AnimationCurve fogDistance = BuildFogDistance();

        [Tooltip("Оттенок тумана по часам. Цвет тумана берётся от неба, это множитель к нему")]
        [GradientUsage(true)] public Gradient fogTint = BuildFogTint();

        // ── Экспозиция ──────────────────────────────────────────────────────

        [Header("Экспозиция")]
        [Tooltip("Вести за собой экспозицию камеры (EV100). По умолчанию выключено: Exposure в профиле " +
                 "выключен, сцена живёт в единицах кадра. Включите — и кадр уйдёт в темноту в 2048 раз, " +
                 "пока кривые не умножены на столько же")]
        public bool driveExposure;

        [Tooltip("EV100 по часам. Больше — темнее кадр (как при дневной съёмке узкой диафрагмой)")]
        public AnimationCurve exposureEv100 = BuildExposure();

        // ── Заливка сцены ───────────────────────────────────────────────────

        [Header("Заливка сцены")]
        [Tooltip("Множитель засветки земли небом (HDRP indirectDiffuseLightingMultiplier) по часам. " +
                 "Ночью меньше: небо остаётся видно, а земля уходит в темноту под фонарик")]
        public AnimationCurve ambientByTime = BuildAmbient();

        // ── Состояние ───────────────────────────────────────────────────────

        /// <summary>Текущий час суток, 0…24.</summary>
        public float Hour => hour;

        /// <summary>
        /// Сколько сейчас света от солнца, в единицах кадра. По нему инспектор
        /// подписывает время суток: часы часами, а в 17:24 солнце уже над
        /// горизонтом низко, и свет закатный.
        /// </summary>
        public float SunLevel =>
            sunLux != null ? Mathf.Max(0f, sunLux.Evaluate(Mathf.Repeat(hour, 24f) / 24f)) : 0f;

        /// <summary>Доля суток, 0…1. По ней считаются все кривые.</summary>
        public float Time01 => hour / 24f;

        /// <summary>
        /// Как называется текущий момент — по свету, а не по часам: в 17:40
        /// солнце ещё выше горизонта, но день уже кончился. Подпись в инспекторе
        /// должна совпадать с картинкой, иначе непонятно, кто врёт.
        /// </summary>
        public string Phase =>
            SunLevel > 1.5f ? "день" :
            SunLevel > 0.05f ? "сумерки" : "ночь";

        public string WeatherName => weather != null ? weather.CurrentName : "—";

        private GradientSky _sky;
        private VolumetricClouds _clouds;
        private Fog _fog;
        private Exposure _exposure;
        private VisualEnvironment _environment;
        private IndirectLightingController _indirect;

        private float _appliedHour = -1f;
        private int _appliedCloudPreset = -1;
        private bool _profileReady;
        private bool _applyQueued;

        /// <summary>
        /// Версия значений по умолчанию. Компонент сравнивает её со своей и, если
        /// отстал, пересобирает градиенты и кривые: так правки кода доезжают до
        /// компонентов, которые уже лежат в сцене.
        /// </summary>
        private const int DefaultsVersion = 4;

        [SerializeField, HideInInspector] private int _defaultsVersion;

        private void Awake()
        {
            // День начинается с того часа, что стоит на ползунке в инспекторе.
            hour = Mathf.Repeat(hour, 24f);

            ResolveReferences();

            // В редакторе профиль тоже нужен — но не экземпляр, а сам ассет:
            // экземпляр уедет в сцену и останется там навсегда.
            if (Application.isPlaying || previewInEditor)
                BuildProfileRefs();

            ApplyNow();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (!paused && dt > 0f)
            {
                float hoursPerSecond = 24f / (Mathf.Max(0.1f, dayLengthMinutes) * 60f);
                float deltaHours = hoursPerSecond * Mathf.Max(0f, timeScale) * dt;

                if (deltaHours > 0f)
                {
                    hour = Mathf.Repeat(hour + deltaHours, 24f);

                    // Погода живёт на тех же часах, что и сутки: при ускорении
                    // времени она должна ускоряться вместе с ним.
                    if (weather != null)
                        weather.Tick(deltaHours * 60f);
                }
            }

            // Профиль дёргается не каждый кадр: смена настроек неба заставляет
            // HDRP перестраивать пробу окружения, а между двумя кадрами небо
            // меняется настолько мало, что этого не видно.
            bool weatherMoving = weather != null && weather.IsTransitioning;
            float hourDelta = Mathf.Abs(Mathf.DeltaAngle(_appliedHour * 15f, hour * 15f) / 15f);

            if (hourDelta > 0.002f || weatherMoving || _appliedHour < 0f)
                ApplyNow();
        }

        /// <summary>Поставить время и сразу применить — так двигают ползунок и фотосъёмка.</summary>
        public void SetHour(float value)
        {
            hour = Mathf.Repeat(value, 24f);

            // В редакторе с выключенным предпросмотром время только меняется:
            // трогать сцену в этом режиме компонент не имеет права.
            if (Application.isPlaying || previewInEditor)
                ApplyNow();
        }

        /// <summary>
        /// Просьба пересчитать сцену: в игре сразу, в редакторе — отложенно,
        /// когда редактор закончит текущую правку. Так погода показывает свои
        /// пресеты прямо в инспекторе, без входа в Play Mode.
        /// </summary>
        public void RequestApply()
        {
            if (Application.isPlaying)
                ApplyNow();
            else
                QueueEditorApply();
        }

        /// <summary>Применить состояние немедленно.</summary>
        public void ApplyNow()
        {
            ResolveReferences();
            EnsureCurves();

            if ((Application.isPlaying || previewInEditor) && !_profileReady)
                BuildProfileRefs();

            WeatherState w = CurrentWeather();
            float t = Time01;

            ApplyCelestial(t, w);
            ApplySky(t, w);
            ApplyFog(t, w);
            ApplyClouds(w);
            ApplyAmbient(t);
            ApplyExposure(t);

            // В редакторе правки лежат в ассете профиля: помечаем его изменённым,
            // иначе сцена и ассет разъедутся при сохранении проекта.
            if (!Application.isPlaying && _profileReady)
                MarkProfileDirty();

            _appliedHour = hour;
        }

        /// <summary>
        /// Погода для расчёта. В игре это живое состояние WeatherController,
        /// в редакторе — пресет, выбранный в нём как стартовый: контроллер в
        /// редакторе не просыпается, и его состояние осталось бы нулевым.
        /// </summary>
        private WeatherState CurrentWeather()
        {
            if (weather == null)
                return DefaultWeather();

            return Application.isPlaying ? weather.State : weather.PreviewState;
        }

        // ── Применение ──────────────────────────────────────────────────────

        private void ApplyCelestial(float t, WeatherState w)
        {
            // Солнце ходит по дуге: 6:00 — горизонт, 12:00 — зенит, 18:00 — снова
            // горизонт, 0:00 — под землёй. Луна идёт в противофазе.
            float pitch = (Hour - 6f) * 15f;

            float dim = Mathf.Lerp(1f, w.sunDimmer, w.Coverage);

            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(pitch, pathAzimuth, 0f);
                sun.color = sunColor.Evaluate(t);
                sun.intensity = Mathf.Max(0f, sunLux.Evaluate(t) * dim);
            }

            if (moon != null)
            {
                moon.transform.rotation = Quaternion.Euler(pitch + 180f, pathAzimuth, 0f);
                moon.color = moonColor.Evaluate(t);
                // Облака гасят и луну, но слабее солнца: тучи светятся ими же.
                moon.intensity = Mathf.Max(0f, moonLux.Evaluate(t) * Mathf.Lerp(1f, w.sunDimmer, w.Coverage * 0.5f));
            }
        }

        private void ApplySky(float t, WeatherState w)
        {
            if (_sky == null)
                return;

            // Страховка от битой кривой: ноль даёт чёрное небо без единой ошибки
            // в консоли. Границы широкие — рабочие значения здесь 0.4–0.9,
            // потому что экспозиции в сцене нет и 1.0 — это уже белый кадр.
            float multiplier = Mathf.Clamp(skyMultiplier.Evaluate(t), 0.02f, 20f);

            Color top = skyTop.Evaluate(t) * multiplier;
            Color middle = skyMiddle.Evaluate(t) * multiplier;
            Color bottom = skyBottom.Evaluate(t) * multiplier;

            // Облака перекрашивают небо, а не подменяют его: закрытое тучами
            // небо — то же небо, только серое. Оттенок погоды подмешиваем в
            // яркости самого неба, иначе пасмурная ночь вышла бы светлее ясной.
            float k = Mathf.Clamp01(w.skyBlend * w.Coverage);
            if (k > 0.001f)
            {
                top = Color.Lerp(top, TintOf(top, w.skyTint), k);
                middle = Color.Lerp(middle, TintOf(middle, w.skyTint), k);
                bottom = Color.Lerp(bottom, TintOf(bottom, w.skyTint), k);
            }

            Set(_sky.skyIntensityMode, SkyIntensityMode.Multiplier);
            Set(_sky.multiplier, multiplier);
            Set(_sky.gradientDiffusion, Mathf.Max(0f, skyDiffusion));
            Set(_sky.top, top);
            Set(_sky.middle, middle);
            Set(_sky.bottom, bottom);
        }

        /// <summary>
        /// Оттенок погоды в яркости самого неба: серый цвет того же порядка,
        /// что и небо в этот час. Так тучи гасят небо, а не светятся поверх него.
        /// </summary>
        private static Color TintOf(Color sky, Color tint)
        {
            float skyLuma = (sky.r + sky.g + sky.b) / 3f;
            float tintLuma = Mathf.Max(0.05f, (tint.r + tint.g + tint.b) / 3f);

            return tint * (skyLuma / tintLuma);
        }

        private void ApplyFog(float t, WeatherState w)
        {
            if (_fog == null)
                return;

            float distance = Mathf.Max(1f, fogDistance.Evaluate(t) * Mathf.Max(0.01f, w.fogScale));

            Set(_fog.enabled, true);
            Set(_fog.meanFreePath, distance);
            Set(_fog.tint, fogTint.Evaluate(t) * w.fogTint);
        }

        private void ApplyClouds(WeatherState w)
        {
            if (_clouds == null)
                return;

            // Простой режим — единственный рабочий на этом профиле. Расширенный
            // и ручной заставляют HDRP печь карту покрытия из текстур cumulus,
            // altostratus и cumulonimbus, а они в профиле пусты: небо осталось
            // бы совсем без облаков, сколько плотности ни выставляй.
            Set(_clouds.cloudControl, VolumetricClouds.CloudControl.Simple);

            if (_appliedCloudPreset != (int)w.preset)
            {
                // Смена формы переписывает плотность, ярусы и кривые облаков.
                // Свои значения кладём сразу следом, ниже, — поэтому порядок
                // здесь важен: сначала пресет, потом всё остальное.
                _clouds.cloudPreset = w.preset;
                _appliedCloudPreset = (int)w.preset;
            }

            Set(_clouds.enable, w.Coverage > 0.02f);
            Set(_clouds.densityMultiplier, Mathf.Clamp(w.density, 0f, 0.95f));
            Set(_clouds.bottomAltitude, Mathf.Max(10f, w.bottomAltitude));
            Set(_clouds.altitudeRange, Mathf.Max(100f, w.altitudeRange));
            Set(_clouds.scatteringTint, w.scatteringTint);
            Set(_clouds.ambientLightProbeDimmer, Mathf.Clamp01(w.ambientDimmer));
            Set(_clouds.sunLightDimmer, Mathf.Clamp01(w.sunDimmer));

            // Ветер — структурный параметр: у него режим и значение в одной паре,
            // и значение нужно писать целиком.
            var wind = _clouds.globalWindSpeed.value;
            wind.mode = WindParameter.WindOverrideMode.Custom;
            wind.customValue = Mathf.Max(0f, w.windSpeedKmh);
            Set(_clouds.globalWindSpeed, wind);

            var orientation = _clouds.orientation.value;
            orientation.mode = WindParameter.WindOverrideMode.Custom;
            orientation.customValue = Mathf.Repeat(w.windOrientation, 360f);
            Set(_clouds.orientation, orientation);

            // Общий ветер сцены: его читают шейдеры и системы частиц, и он должен
            // совпадать с тем, что гонит облака.
            if (_environment != null)
            {
                Set(_environment.windSpeed, Mathf.Max(0f, w.windSpeedKmh));
                Set(_environment.windOrientation, Mathf.Repeat(w.windOrientation, 360f));
            }
        }

        private void ApplyAmbient(float t)
        {
            if (_indirect == null)
                return;

            // Заливка от неба: ночью она и делает кадр ночным, а не синим днём.
            Set(_indirect.indirectDiffuseLightingMultiplier, Mathf.Max(0f, ambientByTime.Evaluate(t)));
        }

        private void ApplyExposure(float t)
        {
            if (!driveExposure || _exposure == null)
                return;

            Set(_exposure.mode, ExposureMode.Fixed);
            Set(_exposure.fixedExposure, exposureEv100.Evaluate(t));
        }

        // ── Служебное ───────────────────────────────────────────────────────

        /// <summary>
        /// Запись в параметр профиля. Состояние переопределения обязательно:
        /// параметр без него лежит в профиле, но объём его не читает — правка
        /// молча уходит в никуда, и искать потом это часами.
        /// </summary>
        private static void Set<T>(VolumeParameter<T> parameter, T value)
        {
            if (parameter == null)
                return;

            parameter.overrideState = true;
            parameter.value = value;
        }

        private void ResolveReferences()
        {
            if (volume == null)
            {
                var volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
                for (int i = 0; i < volumes.Length; i++)
                {
                    if (volumes[i].sharedProfile != null && volumes[i].isGlobal)
                    {
                        volume = volumes[i];
                        break;
                    }
                }
            }

            if (sun == null)
                sun = FindDirectionalLight("Sun", "Directional Light");
            if (moon == null)
                moon = FindDirectionalLight("MoonLight", "Moon");
            if (weather == null)
                weather = Object.FindFirstObjectByType<WeatherController>();
        }

        private static Light FindDirectionalLight(params string[] names)
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);

            for (int n = 0; n < names.Length; n++)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type == LightType.Directional &&
                        string.Equals(lights[i].name, names[n], System.StringComparison.OrdinalIgnoreCase))
                        return lights[i];
                }
            }

            return null;
        }

        private void BuildProfileRefs()
        {
            _profileReady = true;

            if (volume == null || volume.sharedProfile == null)
            {
                Debug.LogWarning("[Lake] Цикл суток: не найден Volume с общим профилем — небо, туман и облака не настраиваются.");
                return;
            }

            bool play = Application.isPlaying;

            // В игре — экземпляр профиля: правки живут до выхода из Play Mode и
            // не трогают ассет. В редакторе экземпляра нет (он уехал бы в сцену),
            // поэтому предпросмотр пишет прямо в ассет — это цена ползунка.
            VolumeProfile profile = play ? volume.profile : volume.sharedProfile;

            profile.TryGet(out _sky);
            profile.TryGet(out _clouds);
            profile.TryGet(out _fog);
            profile.TryGet(out _exposure);
            profile.TryGet(out _environment);
            profile.TryGet(out _indirect);

            if (_indirect == null)
            {
                // Компонента заливки в профиле нет — заводим его сами.
                // В игре он остаётся в экземпляре, в редакторе его нужно
                // приписать к ассету, иначе он пропадёт при первой перезагрузке.
                _indirect = profile.Add<IndirectLightingController>(true);
#if UNITY_EDITOR
                // Официальный путь редактора профиля: компонент-под-ассет нужно
                // приписать к файлу, иначе после перезагрузки ссылка станет пустой.
                if (!play && UnityEditor.EditorUtility.IsPersistent(profile))
                {
                    UnityEditor.AssetDatabase.AddObjectToAsset(_indirect, profile);
                    UnityEditor.EditorUtility.SetDirty(profile);
                    UnityEditor.AssetDatabase.SaveAssets();
                }
#endif
            }

            if (_sky == null)
                Debug.LogWarning("[Lake] Цикл суток: в профиле нет GradientSky — небо не будет меняться. " +
                                 "Добавьте компонент Gradient Sky в профиль или переключите skyType на Gradient.");

            if (_clouds == null)
                Debug.LogWarning("[Lake] Цикл суток: в профиле нет VolumetricClouds — погода не сможет управлять облаками.");
        }

        private void MarkProfileDirty()
        {
#if UNITY_EDITOR
            if (volume != null && volume.sharedProfile != null)
                UnityEditor.EditorUtility.SetDirty(volume.sharedProfile);
#endif
        }

        // ── Значения по умолчанию ───────────────────────────────────────────

        /// <summary>
        /// Пересобрать все градиенты и кривые из значений по умолчанию. Кнопка
        /// «Сбросить цвета и кривые» в инспекторе зовёт именно это.
        /// </summary>
        public void ResetCurves()
        {
            sunColor = BuildSunColor();
            sunLux = BuildSunLux();
            moonColor = BuildMoonColor();
            moonLux = BuildMoonLux();
            skyTop = BuildSkyTop();
            skyMiddle = BuildSkyMiddle();
            skyBottom = BuildSkyBottom();
            skyMultiplier = BuildSkyMultiplier();
            fogDistance = BuildFogDistance();
            fogTint = BuildFogTint();
            exposureEv100 = BuildExposure();
            ambientByTime = BuildAmbient();
            _defaultsVersion = DefaultsVersion;

            // Починка значений — это правка сцены: без пометки Unity о ней
            // не узнает и не сохранит её вместе со сценой.
#if UNITY_EDITOR
            if (!Application.isPlaying && gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        /// <summary>
        /// Страховка на случай пустых полей: сериализация может принести null,
        /// а пустой градиент — молча чёрное небо без единой ошибки в консоли.
        /// </summary>
        private void EnsureCurves()
        {
            if (_defaultsVersion < DefaultsVersion)
                ResetCurves();

            if (sunColor == null) sunColor = BuildSunColor();
            if (sunLux == null) sunLux = BuildSunLux();
            if (moonColor == null) moonColor = BuildMoonColor();
            if (moonLux == null) moonLux = BuildMoonLux();
            if (skyTop == null) skyTop = BuildSkyTop();
            if (skyMiddle == null) skyMiddle = BuildSkyMiddle();
            if (skyBottom == null) skyBottom = BuildSkyBottom();
            if (skyMultiplier == null) skyMultiplier = BuildSkyMultiplier();
            if (fogDistance == null) fogDistance = BuildFogDistance();
            if (fogTint == null) fogTint = BuildFogTint();
            if (exposureEv100 == null) exposureEv100 = BuildExposure();
            if (ambientByTime == null) ambientByTime = BuildAmbient();
        }

        private void OnValidate()
        {
            hour = Mathf.Repeat(hour, 24f);

            // В редакторе компонент не крутится сам, поэтому применять состояние
            // надо руками — но не прямо здесь: OnValidate зовётся в том числе во
            // время загрузки сцены, когда трогать ассеты рано.
            if (!Application.isPlaying)
                QueueEditorApply();
        }

        private void QueueEditorApply()
        {
#if UNITY_EDITOR
            if (_applyQueued)
                return;

            _applyQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                _applyQueued = false;

                if (this == null)
                    return;

                if (previewInEditor)
                    ApplyNow();
            };
#endif
        }

        private static WeatherState DefaultWeather()
        {
            // Запасной вариант, когда WeatherController в сцене не поставлен:
            // средняя облачность и обычный туман, чтобы сцена не осталась
            // ни с чистым небом, ни с молоком.
            return new WeatherState
            {
                preset = VolumetricClouds.CloudPresets.Cloudy,
                density = 0.30f,
                bottomAltitude = 2200f,
                altitudeRange = 1600f,
                coverage = 0.45f,
                scatteringTint = new Color(0.74f, 0.82f, 1f),
                ambientDimmer = 0.7f,
                sunDimmer = 0.85f,
                fogScale = 1f,
                fogTint = Color.white,
                windSpeedKmh = 20f,
                windOrientation = 45f,
                skyTint = new Color(0.5f, 0.56f, 0.7f),
                skyBlend = 0.3f,
            };
        }

        // ── Кривые по умолчанию ─────────────────────────────────────────────
        // Экспозиция сцены не ведётся: Exposure в профиле выключен, поэтому
        // кадр показывает яркость как есть, 1.0 — белый. Отсюда величины:
        // полдень 5, небо от 0.55 (день) до 0.83 (ночь). Все кривые открыты
        // в инспекторе — правьте смело.

        // Unity разрешает в Gradient не больше восьми цветовых ключей: девять
        // и больше — не исключение, а молча проигнорированная запись. Градиент
        // остаётся белым, небо светится как днём в полночь, и виноватую строку
        // приходится искать по консоли.
        private static Gradient MakeGradient(GradientColorKey[] keys)
        {
            var g = new Gradient();
            g.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        private static GradientColorKey Key(float r, float g, float b, float t) =>
            new GradientColorKey(new Color(r, g, b), t);

        private static Gradient BuildSunColor()
        {
            return MakeGradient(new[]
            {
                Key(1.00f, 0.50f, 0.26f, 0.205f),
                Key(1.00f, 0.62f, 0.38f, 0.250f),
                Key(1.00f, 0.80f, 0.62f, 0.300f),
                Key(1.00f, 0.93f, 0.87f, 0.380f),
                Key(1.00f, 0.97f, 0.93f, 0.500f),
                Key(1.00f, 0.90f, 0.82f, 0.650f),
                Key(1.00f, 0.72f, 0.50f, 0.720f),
                Key(0.98f, 0.52f, 0.30f, 0.765f),
            });
        }

        private static AnimationCurve BuildSunLux()
        {
            // Кривая идёт за высотой солнца: восход в 6:00 (0.25), закат в 18:00
            // (0.75), в промежутке — почти синус. Полдень 5 единиц: у земли с
            // альбедо 0.18 это sRGB около 0.68 — светлый день без пересвета.
            //
            // Прежняя версия гасла к 17:30, хотя солнце стоит ещё 9° над
            // горизонтом: вечер выглядел ночью. Теперь свет держится до заката.
            return new AnimationCurve(
                new Keyframe(0.000f, 0f),
                new Keyframe(0.240f, 0f),
                new Keyframe(0.250f, 0.06f),
                new Keyframe(0.275f, 0.60f),
                new Keyframe(0.310f, 1.60f),
                new Keyframe(0.360f, 3.00f),
                new Keyframe(0.420f, 4.30f),
                new Keyframe(0.500f, 5.00f),
                new Keyframe(0.580f, 4.30f),
                new Keyframe(0.640f, 3.00f),
                new Keyframe(0.690f, 1.60f),
                new Keyframe(0.725f, 0.60f),
                new Keyframe(0.750f, 0.06f),
                new Keyframe(0.760f, 0f),
                new Keyframe(1.000f, 0f));
        }

        private static Gradient BuildMoonColor()
        {
            return MakeGradient(new[]
            {
                Key(0.55f, 0.66f, 1.00f, 0.00f),
                Key(0.68f, 0.76f, 1.00f, 0.50f),
                Key(0.55f, 0.66f, 1.00f, 1.00f),
            });
        }

        private static AnimationCurve BuildMoonLux()
        {
            // Луна остаётся заполнителем: ночь держат фонарик и небо. Настоящая
            // луна в зените — 0.3 лк, в этих единицах это 0.00015, то есть
            // ничего. Кому нужна лунная ночь — поднимайте кривую до 0.02–0.05.
            return new AnimationCurve(
                new Keyframe(0.000f, 0.00017f),
                new Keyframe(0.250f, 0.00007f),
                new Keyframe(0.750f, 0.00007f),
                new Keyframe(1.000f, 0.00017f));
        }

        private static Gradient BuildSkyTop()
        {
            // Ночь в конце и начале — один и тот же цвет: кривая замкнута на сутки,
            // и на стыке 24:00 — 0:00 неба не должно дёргаться.
            return MakeGradient(new[]
            {
                Key(0.030f, 0.055f, 0.140f, 0.000f),
                Key(0.045f, 0.070f, 0.170f, 0.210f),
                Key(0.110f, 0.110f, 0.230f, 0.250f),
                Key(0.190f, 0.280f, 0.560f, 0.330f),
                Key(0.230f, 0.420f, 0.880f, 0.500f),
                Key(0.190f, 0.280f, 0.560f, 0.670f),
                Key(0.130f, 0.115f, 0.230f, 0.750f),
                Key(0.035f, 0.058f, 0.145f, 0.800f),
            });
        }

        private static Gradient BuildSkyMiddle()
        {
            return MakeGradient(new[]
            {
                Key(0.055f, 0.085f, 0.190f, 0.000f),
                Key(0.075f, 0.100f, 0.210f, 0.210f),
                Key(0.240f, 0.170f, 0.220f, 0.250f),
                Key(0.330f, 0.420f, 0.680f, 0.330f),
                Key(0.480f, 0.640f, 0.960f, 0.500f),
                Key(0.330f, 0.420f, 0.680f, 0.670f),
                Key(0.280f, 0.180f, 0.200f, 0.750f),
                Key(0.062f, 0.088f, 0.195f, 0.800f),
            });
        }

        private static Gradient BuildSkyBottom()
        {
            return MakeGradient(new[]
            {
                Key(0.080f, 0.105f, 0.200f, 0.000f),
                Key(0.130f, 0.140f, 0.230f, 0.210f),
                Key(0.420f, 0.240f, 0.190f, 0.250f),
                Key(0.520f, 0.520f, 0.620f, 0.330f),
                Key(0.640f, 0.720f, 0.900f, 0.500f),
                Key(0.520f, 0.520f, 0.620f, 0.670f),
                Key(0.520f, 0.260f, 0.170f, 0.750f),
                Key(0.140f, 0.145f, 0.225f, 0.800f),
            });
        }

        private static AnimationCurve BuildSkyMultiplier()
        {
            // Множитель неба в HDRP. Ночью он тянет вверх тёмные цвета (0.83),
            // днём опускает светлые (0.55): без этого небо либо чёрное, либо
            // выбеленное, середины нет. Экспозиция сцены не ведётся.
            return new AnimationCurve(
                new Keyframe(0.000f, 0.83f),
                new Keyframe(0.210f, 0.70f),
                new Keyframe(0.250f, 0.62f),
                new Keyframe(0.300f, 0.56f),
                new Keyframe(0.500f, 0.55f),
                new Keyframe(0.700f, 0.56f),
                new Keyframe(0.750f, 0.62f),
                new Keyframe(0.790f, 0.72f),
                new Keyframe(1.000f, 0.83f));
        }

        private static AnimationCurve BuildFogDistance()
        {
            return new AnimationCurve(
                new Keyframe(0.000f, 22f),
                new Keyframe(0.210f, 30f),
                new Keyframe(0.290f, 45f),
                new Keyframe(0.500f, 90f),
                new Keyframe(0.700f, 45f),
                new Keyframe(0.800f, 28f),
                new Keyframe(1.000f, 22f));
        }

        private static Gradient BuildFogTint()
        {
            return MakeGradient(new[]
            {
                Key(0.55f, 0.68f, 1.00f, 0.000f),
                Key(0.60f, 0.70f, 1.00f, 0.220f),
                Key(1.00f, 0.72f, 0.62f, 0.260f),
                Key(0.95f, 0.95f, 0.98f, 0.360f),
                Key(0.92f, 0.95f, 1.00f, 0.500f),
                Key(0.95f, 0.95f, 0.98f, 0.660f),
                Key(1.00f, 0.70f, 0.58f, 0.740f),
                Key(0.60f, 0.70f, 1.00f, 0.800f),
            });
        }

        private static AnimationCurve BuildExposure()
        {
            return new AnimationCurve(
                new Keyframe(0.000f, 10f),
                new Keyframe(0.250f, 10.5f),
                new Keyframe(0.500f, 9f),
                new Keyframe(0.750f, 10.5f),
                new Keyframe(1.000f, 10f));
        }

        private static AnimationCurve BuildAmbient()
        {
            // Это множитель засветки земли небом. Ночью он и делает кадр ночным:
            // само небо видно (множитель неба 0.83), а земля остаётся тёмной.
            return new AnimationCurve(
                new Keyframe(0.000f, 0.20f),
                new Keyframe(0.220f, 0.22f),
                new Keyframe(0.260f, 0.60f),
                new Keyframe(0.350f, 0.85f),
                new Keyframe(0.500f, 0.95f),
                new Keyframe(0.700f, 0.85f),
                new Keyframe(0.760f, 0.55f),
                new Keyframe(0.820f, 0.25f),
                new Keyframe(1.000f, 0.20f));
        }
    }
}
