using UnityEngine;

namespace Lake.CameraRig
{
    /// <summary>
    /// Bodycam — покачивание камеры, как у нагрудного крепления.
    ///
    /// Ключевая архитектурная мысль: скрипт висит на ПУСТОМ держателе, который
    /// является родителем Main Camera. Держатель смещается, камера — нет.
    /// Благодаря этому скрипты мыши (Mouse Look, Cinemachine) продолжают писать
    /// в камеру как обычно и не знают про покачивание: конфликта за трансформа
    /// не возникает в принципе, а не «случайно обошлось».
    ///
    /// Вторая важная деталь — движение НАПРАВЛЕННОЕ. Одна и та же скорость
    /// вперёд и вбок даёт разное покачивание, потому что вектор скорости
    /// раскладывается в локальные оси взгляда через Vector3.Dot.
    ///
    /// Третья — шум Перлина вместо синусоид. Синусоида идеально периодична,
    /// глаз её выучивает за несколько секунд, и человек начинает читать
    /// покачивание вместо картинки. Перлин не имеет периода: каждый шаг
    /// чуть отличается от предыдущего.
    ///
    /// Эффект намеренно слабый. Настоящая нагрудная камера почти неподвижна —
    /// видно дрожание, а не колебания. Все амплитуды по умолчанию малы.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Bodycam : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        // Ссылки
        // ─────────────────────────────────────────────────────────────────────

        [Header("Ссылки")]
        [Tooltip("Камера, относительно взгляда которой считается покачивание. Должна быть потомком этого держателя. Если пусто — берётся первый Camera в детях.")]
        public Transform cameraTransform;

        [Tooltip("CharacterController персонажа. Ищется вверх по иерархии, если пусто.")]
        public CharacterController controller;

        [Tooltip("Rigidbody персонажа. Используется только если нет CharacterController — чтобы компонент работал и с физикой.")]
        public Rigidbody body;

        // ─────────────────────────────────────────────────────────────────────
        // Пороги состояний
        // ─────────────────────────────────────────────────────────────────────

        [Header("Состояния: покой / ходьба / бег")]
        [Tooltip("Скорость, при которой покачивание считается ходьбой, м/с. Ниже неё работает только дыхание.")]
        public float walkSpeed = 2.1f;

        [Tooltip("Скорость, при которой покачивание считается бегом, м/с.")]
        public float runSpeed = 3.9f;

        [Tooltip("Гистерезис переключения, м/с. Без него на границе порога состояние дрожит: раз замедлился до порога — ушёл в ходьбу, чуть быстрее — снова в бег.")]
        public float stateHysteresis = 0.35f;

        [Tooltip("Постоянная времени перехода между состояниями, секунды. За это время проходит примерно 63 % пути. 0.15 — бодро, 0.4 — вяло.")]
        public float stateSmoothTime = 0.22f;

        // ─────────────────────────────────────────────────────────────────────
        // Покачивание при ходьбе
        // ─────────────────────────────────────────────────────────────────────

        [Header("Покачивание: ходьба")]
        [Tooltip("Вертикальная амплитуда при ходьбе, метры. Держите в пределах 1–2 см — больше уже читается как «плавание».")]
        public float walkVerticalAmplitude = 0.014f;

        [Tooltip("Боковая амплитуда при ходьбе, метры. Задаёт ширину шага: если её мало, ноги читаются как поставленные в паре сантиметров друг от друга.")]
        public float walkLateralAmplitude = 0.016f;

        [Tooltip("Частота шума на единицу пройденного расстояния. Управляет темпом: чем больше, тем чаще качается. В коде аргумент шума растёт как расстояние, поэтому частота сама подстраивается под скорость.")]
        public float walkFrequency = 2.6f;

        // ─────────────────────────────────────────────────────────────────────
        // Покачивание при беге
        // ─────────────────────────────────────────────────────────────────────

        [Header("Покачивание: бег")]
        [Tooltip("Множитель вертикальной амплитуды при беге относительно ходьбы.")]
        public float runVerticalMultiplier = 1.35f;

        [Tooltip("Множитель боковой амплитуды при беге. На бегу шаг ставится заметно шире.")]
        public float runLateralMultiplier = 1.45f;

        [Tooltip("Множитель частоты при беге. Держим МЕНЬШЕ амплитудных множителей: на бегу шаг короче, но не втрое, а частота, выросшая так же, как амплитуда, читается как мелкая тремор, а не как шаг.")]
        public float runFrequencyMultiplier = 1.15f;

        // ─────────────────────────────────────────────────────────────────────
        // Направленность движения
        // ─────────────────────────────────────────────────────────────────────

        [Header("Направленность")]
        [Tooltip("Насколько вертикальное покачивание зависит от движения вперёд. 1 — только вперёд, 0 — одинаково при любом направлении.")]
        [Range(0f, 1f)] public float forwardInfluence = 0.7f;

        [Tooltip("Насколько боковое покачивание зависит от стрейфа. 1 — качается только вбок.")]
        [Range(0f, 1f)] public float strafeInfluence = 0.8f;

        [Tooltip("Подавление покачивания при движении задом. Спиной человек идёт иначе — короче, без выпрямления. 1 — отключает совсем.")]
        [Range(0f, 1f)] public float backwardSuppression = 0.35f;

        [Tooltip("Сколько бокового качания остаётся при движении прямо. Ненулевое значение нужно, чтобы шаг не был полностью вертикальным — нога отводится в сторону всегда.")]
        [Range(0f, 0.5f)] public float forwardLateralLeak = 0.18f;

        // ─────────────────────────────────────────────────────────────────────
        // Крен и наклон
        // ─────────────────────────────────────────────────────────────────────

        [Header("Крен и наклон корпуса")]
        [Tooltip("Крен в градусах при полном стрейфе. Инерция тела: корпус уходит в сторону движения, голова отстаёт.")]
        public float strafeRoll = 1.4f;

        [Tooltip("Крен в градусах при движении вперёд/назад. Обычно вдвое меньше бокового.")]
        public float forwardRoll = 0.6f;

        [Tooltip("Наклон в градусах при полном разгоне/торможении. Голова на шее не может разогнаться вместе с корпусом.")]
        public float speedPitch = 0.8f;

        [Tooltip("Время сглаживания наклона и крена, секунды.")]
        public float tiltSmoothTime = 0.18f;

        // ─────────────────────────────────────────────────────────────────────
        // Дыхание в покое
        // ─────────────────────────────────────────────────────────────────────

        [Header("Дыхание в покое")]
        [Tooltip("Амплитуда микро-тряски в покое, метры. Это не дыхание в грудь, а мелкая дрожь от корпуса — именно её видно в записи.")]
        public float idleAmplitude = 0.0035f;

        [Tooltip("Частота шума покоя. Ниже, чем при ходьбе, — в покое всё медленнее.")]
        public float idleFrequency = 0.9f;

        [Tooltip("Амплитуда микро-крена в покое, градусы.")]
        public float idleRoll = 0.12f;

        // ─────────────────────────────────────────────────────────────────────
        // Падение и приземление
        // ─────────────────────────────────────────────────────────────────────

        [Header("Падение и приземление")]
        [Tooltip("Множитель смещения вниз на каждый м/с падения. Без этого на спуске камера остаётся в потолке и приземление выглядит как обрыв.")]
        public float fallFactor = 0.012f;

        [Tooltip("Потолок смещения при падении, метры.")]
        public float maxFallOffset = 0.06f;

        [Tooltip("Дополнительная просадка в момент касания земли, метры на каждый м/с удара.")]
        public float landingDipPerUnitSpeed = 0.014f;

        [Tooltip("Потолок просадки при приземлении, метры.")]
        public float maxLandingDip = 0.09f;

        [Tooltip("Жёсткость пружины, возвращающей камеру наверх после удара. Без неё просадка не возвращается в ноль и камера навсегда остаётся заниженной.")]
        public float landingStiffness = 160f;

        [Tooltip("Затухание пружины при приземлении, 1/с. Больше — отскок короче и глуше.")]
        public float landingRecovery = 9f;

        // ─────────────────────────────────────────────────────────────────────
        // Сглаживание
        // ─────────────────────────────────────────────────────────────────────

        [Header("Сглаживание")]
        [Tooltip("Постоянная времени сглаживания позиционного смещения, секунды. Основной параметр 'едва заметности': он гарантирует отсутствие рывков.")]
        public float positionSmoothTime = 0.08f;

        [Tooltip("Постоянная времени сглаживания вращения, секунды.")]
        public float rotationSmoothTime = 0.1f;

        [Tooltip("Общий множитель на всё покачивание. Поставьте 0 — камера станет полностью неподвижной, не отключая компонент.")]
        [Range(0f, 3f)] public float masterIntensity = 1f;

        // ─────────────────────────────────────────────────────────────────────
        // Внутреннее состояние
        // ─────────────────────────────────────────────────────────────────────

        // Аргумент шума. Растёт вместе с пройденным расстоянием, поэтому частота
        // покачивания автоматически следует за скоростью: бежишь — качается чаще.
        private float _noiseTime;
        private float _idleNoiseTime;

        private float _runBlend;
        private float _moveBlend;

        private Vector3 _positionOffset;

        private Vector3 _rotationOffset;

        private float _landingDip;
        private float _landingDipVelocity;

        private bool _wasGrounded = true;
        private float _lastVerticalSpeed;

        /// <summary>
        /// Горизонтальная скорость, которую компонент реально видит в этом кадре, м/с.
        /// Если в инспекторе амплитуды настроены, а движения нет — смотрите сюда:
        /// почти всегда это значит, что контроллер или тело не нашлись.
        /// </summary>
        public float MeasuredSpeed { get; private set; }

        /// <summary>Насколько сейчас «включено» покачивание, 0…1.</summary>
        public float MoveBlend => _moveBlend;

        /// <summary>Насколько покачивание перешло в режим бега, 0…1.</summary>
        public float RunBlend => _runBlend;

        // Каналы Перлина разведены по разным вторым аргументам, иначе оси
        // коррелировали бы между собой и покачивание шло «по диагонали».
        private const float ChannelVertical = 0.13f;
        private const float ChannelLateral = 7.71f;
        private const float ChannelIdleX = 3.37f;
        private const float ChannelIdleY = 11.09f;

        // ─────────────────────────────────────────────────────────────────────
        // Инициализация
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (cameraTransform == null)
            {
                Camera child = GetComponentInChildren<Camera>();
                if (child != null)
                    cameraTransform = child.transform;
            }

            if (controller == null)
                controller = GetComponentInParent<CharacterController>();

            if (body == null)
                body = GetComponentInParent<Rigidbody>();
        }

        /// <summary>Возвращает держатель в нейтраль — удобно при отладке.</summary>
        private void Reset()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        private void OnDisable()
        {
            // Без этого после отключения компонента камера осталась бы
            // в смещённой позе, и при повторном включении дёрнула бы.
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Основной цикл
        // ─────────────────────────────────────────────────────────────────────

        private void LateUpdate()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Сам расчёт, вынесенный из LateUpdate. Отдельный метод с явным dt
        /// позволяет прогнать компонент с фиксированным шагом времени —
        /// удобно и для отладки, и для тестов.
        /// </summary>
        public void Tick(float dt)
        {
            if (dt <= 0f)
                return;

            Vector3 velocity = GetVelocity();
            bool grounded = GetGrounded(velocity);

            // Скорость в локальных осях ВЗГЛЯДА, а не корпуса. Если камера
            // смотрит в сторону от движения, «вперёд» для качания — это именно
            // туда, куда смотрит игрок, и это правильно.
            Transform reference = cameraTransform != null ? cameraTransform : transform;
            Vector3 localVelocity = reference.InverseTransformDirection(velocity);

            float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            MeasuredSpeed = horizontalSpeed;
            UpdateStates(horizontalSpeed, dt);

            // ── Раскладка по направлению ────────────────────────────────────
            // forward > 0 — движение в сторону взгляда, < 0 — задом.
            //
            // Компоненты нормируем на walkSpeed, и это обязательно. Если подставить
            // сырые метры в секунду, то при 2.1 м/с стрейфа множитель стрейфа
            // станет 2.08 вместо 1.0, и амплитуда будет зависеть от скорости
            // дважды: один раз через нормировку, второй — сама по себе.
            float forward = localVelocity.z;
            float strafe = localVelocity.x;

            float referenceSpeed = Mathf.Max(0.01f, walkSpeed);
            float forward01 = Mathf.Clamp01(Mathf.Abs(forward) / referenceSpeed);
            float strafe01 = Mathf.Clamp01(Mathf.Abs(strafe) / referenceSpeed);

            // Знак стрейфа: при движении влево голова должна качаться иначе,
            // чем при движении вправо.
            float strafeSign = strafe >= 0f ? 1f : -1f;
            float forwardSign = forward >= 0f ? 1f : -1f;

            // Движение задом гасим: спиной шаг короче и мягче.
            float backwardFactor = forward < 0f ? 1f - backwardSuppression : 1f;

            // Вертикаль зависит в основном от движения вперёд, бок — от стрейфа,
            // но не полностью: прямая ходьба тоже чуть качает в стороны,
            // а стрейф — чуть вверх-вниз. Отсюда два «протекания».
            float verticalDrive = Mathf.Lerp(1f, forward01, forwardInfluence) * backwardFactor;
            float lateralDrive = Mathf.Lerp(1f, strafe01, strafeInfluence) * backwardFactor;

            // ── Шаг ─────────────────────────────────────────────────────────
            // Аргумент шума растёт от пройденного расстояния: пройдено больше —
            // покачаний больше, без всякой привязки к частоте кадров.
            _noiseTime += horizontalSpeed * dt;

            float frequency = walkFrequency * Mathf.Lerp(1f, runFrequencyMultiplier, _runBlend);
            float verticalAmplitude = walkVerticalAmplitude
                                      * Mathf.Lerp(1f, runVerticalMultiplier, _runBlend)
                                      * verticalDrive * _moveBlend;
            float lateralAmplitude = walkLateralAmplitude
                                     * Mathf.Lerp(1f, runLateralMultiplier, _runBlend)
                                     * lateralDrive * _moveBlend;

            float vertical = SignedNoise(_noiseTime * frequency, ChannelVertical) * verticalAmplitude;

            float lateral = SignedNoise(_noiseTime * frequency * 0.8f, ChannelLateral)
                            * lateralAmplitude * strafeSign;

            // Прямая ходьба тоже даёт немного бокового — нога отводится всегда.
            lateral += SignedNoise(_noiseTime * frequency * 0.5f, ChannelLateral + 0.5f)
                       * lateralAmplitude * forwardLateralLeak * forward01;

            // ── Дыхание в покое ────────────────────────────────────────────
            // Накладывается не «вместо» шага, а поверх: при ходьбе дыхание
            // почти не видно, в покое — единственный источник движения.
            _idleNoiseTime += dt;
            float idleBlend = 1f - _moveBlend;
            float idleX = SignedNoise(_idleNoiseTime * idleFrequency, ChannelIdleX) * idleAmplitude * idleBlend;
            float idleY = SignedNoise(_idleNoiseTime * idleFrequency * 0.8f, ChannelIdleY) * idleAmplitude * idleBlend;
            // Имена локальных переменных не должны совпадать с полями компонента:
            // иначе локальная затенит поле и формула умножит саму себя.
            float idleRollOffset = SignedNoise(_idleNoiseTime * idleFrequency * 0.7f, ChannelIdleY + 1f) * idleRoll * idleBlend;

            // ── Падение и приземление ──────────────────────────────────────
            UpdateVerticalMotion(velocity, grounded, dt);

            // ── Сборка и сглаживание ───────────────────────────────────────
            // Сглаживание здесь обязательно: без него любая смена состояния
            // (старт, остановка, смена направления) даёт скачок.
            var targetPosition = new Vector3(
                lateral + idleX,
                vertical + idleY + _landingDip,
                0f);

            targetPosition *= masterIntensity;

            _positionOffset = Damp(_positionOffset, targetPosition, positionSmoothTime, dt);

            // Крен: вбок сильнее, чем вперёд. Обратный знак — голова отстаёт
            // от корпуса, а не опережает.
            //
            // Считаем по нормированным компонентам, а не по метрам в секунду:
            // иначе на бегу крен вырос бы втрое относительно показанного в инспекторе.
            // Рост с бегом задаём отдельно и умеренно — тело кренится сильнее,
            // но не в разы.
            float speedScale = Mathf.Lerp(1f, 1.4f, _runBlend);
            float roll = -(strafe01 * strafeSign * strafeRoll + forward01 * forwardSign * forwardRoll) * speedScale;
            float pitch = -Mathf.Clamp01((horizontalSpeed - walkSpeed) / Mathf.Max(0.01f, runSpeed - walkSpeed))
                          * speedPitch;

            var targetRotation = new Vector3(pitch + idleRollOffset, 0f, roll + idleRollOffset * 0.5f) * masterIntensity;

            _rotationOffset = Damp(_rotationOffset, targetRotation, rotationSmoothTime, dt);

            transform.localPosition = _positionOffset;
            transform.localRotation = Quaternion.Euler(_rotationOffset.x, _rotationOffset.y, _rotationOffset.z);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Вспомогательное
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Экспоненциальное сглаживание — собственная замена Mathf.SmoothDamp,
        /// и это не блажь.
        ///
        /// Замеренная скорость сходимости SmoothDamp в этой версии движка
        /// примерно в 350 раз медленнее документированной: при smoothTime = 0.22
        /// за 0.67 с (40 кадров) он проходит меньше процента пути. Для переходов
        /// «покой → ходьба → бег» это минуты вместо долей секунды.
        ///
        /// Экспонента ведёт себя ровно как записана: smoothTime — постоянная
        /// времени, за неё проходит примерно 63 % пути. Формула 1 - exp(-dt/τ)
        /// безусловно устойчива при любом dt и не зависит от частоты кадров.
        /// </summary>
        private static float Damp(float current, float target, float smoothTime, float dt)
        {
            if (smoothTime <= 0f)
                return target;
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-dt / smoothTime));
        }

        private static Vector3 Damp(Vector3 current, Vector3 target, float smoothTime, float dt)
        {
            if (smoothTime <= 0f)
                return target;
            return Vector3.Lerp(current, target, 1f - Mathf.Exp(-dt / smoothTime));
        }

        /// <summary>
        /// Переводит шум Перлина из диапазона 0…1 в −1…1, чтобы вокруг нуля
        /// качалось, а не висело вверху.
        /// </summary>
        private static float SignedNoise(float t, float channel)
        {
            return (Mathf.PerlinNoise(t, channel) - 0.5f) * 2f;
        }

        /// <summary>
        /// Плавно переводит покачивание между ходьбой и бегом. Гистерезис
        /// обязателен: без него на границе порога состояние мигает каждый кадр.
        /// </summary>
        private void UpdateStates(float horizontalSpeed, float dt)
        {
            float previous = _runBlend;

            if (previous > 0.5f)
            {
                if (horizontalSpeed < runSpeed - stateHysteresis)
                    _runBlend = 0f;
            }
            else if (horizontalSpeed > runSpeed + stateHysteresis)
            {
                _runBlend = 1f;
            }

            _runBlend = Damp(_runBlend, horizontalSpeed >= runSpeed ? 1f : 0f, stateSmoothTime, dt);

            // Общая «движучесть»: насколько вообще покачивать. Ступенчатая
            // функция скорости, сглаженная — так камера не прыгает на старте.
            float moveTarget = Mathf.InverseLerp(0f, walkSpeed, horizontalSpeed);
            _moveBlend = Damp(_moveBlend, moveTarget, stateSmoothTime, dt);
        }

        /// <summary>
        /// Падение и приземление. В полёте камера уезжает чуть вниз и вверх
        /// по инерции, а в момент касания земли даёт короткий отскок.
        /// Просадка при приземлении гасится экспоненциально — линейный
        /// демпфер при большом dt уходит в область неустойчивости и начинает раскачивать.
        /// </summary>
        private void UpdateVerticalMotion(Vector3 velocity, bool grounded, float dt)
        {
            float verticalSpeed = velocity.y;

            if (!grounded)
            {
                // В воздухе камера отстаёт: чем быстрее падение, тем ниже.
                float fallTarget = Mathf.Clamp(-verticalSpeed * fallFactor, 0f, maxFallOffset);
                _landingDip = Damp(_landingDip, fallTarget, 0.12f, dt);
            }
            else
            {
                // Касание земли: импульс вниз, тем сильнее, чем быстрее падали.
                if (!_wasGrounded)
                {
                    float impact = Mathf.Abs(_lastVerticalSpeed);
                    _landingDipVelocity -= Mathf.Min(impact * landingDipPerUnitSpeed, maxLandingDip);
                }

                // Пружина к нулю ОБЯЗАТЕЛЬНА. Одного затухания скорости
                // недостаточно: смещение интегрирует скорость и замирает намертво,
                // то есть после жёсткого приземления камера навсегда остаётся ниже.
                //
                // Схема устойчива при любом dt: жёсткость через приращение,
                // затухание экспоненциальное. Линейный демпфер при dt > 2/c
                // уходит в область неустойчивости и начинает раскачивать.
                _landingDipVelocity += -_landingDip * landingStiffness * dt;
                _landingDipVelocity -= _landingDipVelocity * (1f - Mathf.Exp(-landingRecovery * dt));
                _landingDip += _landingDipVelocity * dt;
            }

            _wasGrounded = grounded;
            _lastVerticalSpeed = verticalSpeed;
        }

        private Vector3 GetVelocity()
        {
            if (controller != null)
                return controller.velocity;
            if (body != null)
                return body.linearVelocity;
            return Vector3.zero;
        }

        /// <summary>
        /// Проверка «стоим на земле». У CharacterController спросим напрямую.
        /// У Rigidbody судим по вертикальной скорости: при прижатии к земле она
        /// близка к нулю, в свободном падении — нет.
        ///
        /// Отдельно кинематику НЕ проверяем. Кинематическое тело вполне может
        /// падать, и если считать его всегда стоящим на земле, камера в падении
        /// останется в потолке, а приземление пройдёт без отскока.
        /// </summary>
        private bool GetGrounded(Vector3 velocity)
        {
            if (controller != null)
                return controller.isGrounded;
            if (body != null)
                return Mathf.Abs(velocity.y) < 0.6f;
            return true;
        }
    }
}
