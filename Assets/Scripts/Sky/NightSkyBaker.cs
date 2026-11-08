using System;
using UnityEngine;

namespace Lake.Sky
{
    /// <summary>
    /// Процедурное ночное небо: градиент атмосферы, Млечный Путь и звёзды
    /// с распределением по блеску.
    ///
    /// Пишется в HDR-кубомапу, поэтому яркости многократно выше единицы:
    /// дальше их съедает экспозиция проекта (EV100 = 10, то есть единице
    /// линейного значения соответствует 1024 нит).
    ///
    /// Звёзды НЕ считаются перебором на каждый пиксель — при 3000 звёзд это
    /// миллиарды операций. Фон считается попиксельно, а звёзды рисуются
    /// аддитивными сплатами: проекция на грань и заливка маленького кружка.
    ///
    /// Генерация детерминированная: тот же seed даёт то же небо, поэтому
    /// ассет можно перегенерировать без «прыжков» между запусками.
    /// </summary>
    public static class NightSkyBaker
    {
        /// <summary>Направление на луну. Вынесено в билборд, а не в кубомапу.</summary>
        public static readonly Vector3 MoonDirection = new Vector3(0.42f, 0.24f, -0.87f).normalized;

        private const int NoiseGrid = 32;

        public struct Star
        {
            public Vector3 Direction;
            /// <summary>Пиковая яркость в нитах.</summary>
            public float Luminance;
            /// <summary>Радиус кружка в радианах. В кубомапе это 1–4 пикселя.</summary>
            public float Radius;
            /// <summary>Сила дифракционных лучей, 0 — нет.</summary>
            public float Spike;
            public Color Tint;
        }

        private static Star[] _stars;
        private static float[] _noise;

        /// <summary>
        /// Множитель яркости звёзд, подобран под экспозицию проекта.
        /// Слабая звезда даёт ~62 нит, яркая — до 4700 нит и уходит в белое
        /// вместе с дифракционными лучами, как настоящая.
        /// </summary>
        private const float StarBrightness = 520f;

        /// <summary>Множитель яркости Млечного Пути: пик около 63 нит.</summary>
        private const float MilkyWayBrightness = 150f;

        /// <summary>Плоскость галактики наклонена к горизонту.</summary>
        private static Vector3 GalacticNormal = new Vector3(0.34f, 0.62f, 0.71f).normalized;

        public static Star[] Stars => _stars;

        /// <summary>
        /// Разброс звёзд и сетка шума. Детерминирована по seed.
        /// </summary>
        public static void Init(int seed, int starCount)
        {
            var random = new System.Random(seed);

            _noise = new float[NoiseGrid * NoiseGrid];
            for (int i = 0; i < _noise.Length; i++)
                _noise[i] = (float)random.NextDouble();

            GalacticNormal = new Vector3(
                (float)(random.NextDouble() * 2.0 - 1.0),
                (float)(random.NextDouble() * 2.0 - 1.0),
                (float)(random.NextDouble() * 2.0 - 1.0)).normalized;

            _stars = new Star[starCount];
            for (int i = 0; i < starCount; i++)
            {
                // Равномерное распределение по сфере через кубический корень:
                // равномерная лотерея сгруппировала бы звёзды у полюсов.
                float u = (float)(random.NextDouble() * 2.0 - 1.0);
                float theta = (float)(random.NextDouble() * Math.PI * 2.0);
                float s = Mathf.Sqrt(Mathf.Max(0f, 1f - u * u));
                var dir = new Vector3(s * Mathf.Cos(theta), u, s * Mathf.Sin(theta)).normalized;

                // Распределение по блеску: тусклых звёзд несравнимо больше.
                // Обратная степень даёт ровно ту долю ярких, что бывает в небе.
                double mag = random.NextDouble();

                // Калибровка под экспозицию проекта (EV100 = 10, то есть единице
                // линейного значения соответствует 1024 нит).
                //
                // Настоящее ночное небо в миллион раз темнее Луны, и при такой
                // экспозиции оно было бы физически не видно: первая сборка дала
                // максимум 0,4 нит, то есть 0,0004 линейного значения и чёрный
                // кадр. Поэтому яркости подняты до видимых величин — так же, как
                // это делают во всех играх: глаз адаптируется к темноте локально,
                // и ночное небо в реальности выглядит ярче, чем следует из
                // абсолютных значений.
                float lum = (float)(0.12 + 9.0 * Math.Pow(mag, 5.5)) * StarBrightness;

                // Цвет по температуре: голубоватые горячие, оранжевые холодные.
                double t = random.NextDouble();
                var tint = t < 0.12 ? new Color(0.72f, 0.80f, 1.00f)
                        : t < 0.42 ? new Color(0.88f, 0.92f, 1.00f)
                        : t < 0.78 ? new Color(1.00f, 0.98f, 0.94f)
                        : new Color(1.00f, 0.86f, 0.68f);

                // Звезда должна быть ТОЧКОЙ, а не пятном. Радиус подобран так,
                // чтобы слабая звезда занимала около одного текселя в карте
                // 2048x1024 (один тексель = 0.176°). Первая сборка дала радиус
                // 0.0024–0.0066 рад при множителе сплата 2.2, и звёзды
                // разрастались до 8–12 текселей — небо выглядело как снег.
                float radius = (float)(0.0009 + 0.0018 * Math.Pow(mag, 2.2));

                // Дифракционные лучи есть только у самых ярких.
                float spike = lum > 11f ? (float)(0.30 + random.NextDouble() * 0.60) : 0f;

                _stars[i] = new Star
                {
                    Direction = dir,
                    Luminance = lum,
                    Radius = radius,
                    Spike = spike,
                    Tint = tint
                };
            }
        }

        /// <summary>
        /// Фон неба в направлении <paramref name="direction"/>: градиент атмосферы
        /// и Млечный Путь. Без звёзд и луны — они рисуются сплатами.
        /// </summary>
        public static Color BackgroundRadiance(Vector3 direction)
        {
            var d = direction.normalized;
            float up = d.y;

            // У горизонта светлее из-за рассеяния, к зениту почти чёрный.
            // Тот же механизм, что и в настоящем небе, поэтому ночь не
            // выглядит равномерной заливкой.
            float horizon = Mathf.Clamp01(1f - Mathf.Abs(up) * 3.2f);
            float zenith = Mathf.Clamp01(up);

            // Значения в нитах, откалиброванные под EV100 = 10:
            // зенит около 5–16 нит (почти чёрный), горизонт до 35–62 нит
            // (тёмно-синий, читаемый).
            var sky = new Color(1f, 1f, 1f, 1f);
            sky.r = 5.5f + horizon * 30.0f + zenith * 2.2f;
            sky.g = 7.7f + horizon * 39.0f + zenith * 4.0f;
            sky.b = 14.5f + horizon * 62.0f + zenith * 10.5f;

            // Свечение атмосферы вдоль горизонта.
            float airglow = Mathf.Exp(-Mathf.Abs(up) * 9f) * horizon;
            sky.g += airglow * 15f;
            sky.b += airglow * 25f;

            AddMilkyWay(d, ref sky);
            return sky;
        }

        /// <summary>
        /// Млечный Путь: полоса вдоль плоскости галактики с пылевыми
        /// прожилками. Без пыли полоса выглядит как размазанная кисть.
        /// </summary>
        private static void AddMilkyWay(Vector3 d, ref Color sky)
        {
            float dist = Mathf.Abs(Vector3.Dot(d, GalacticNormal));

            float band = Mathf.Exp(-Mathf.Pow(dist / 0.20f, 2.0f));
            if (band < 0.004f)
                return;

            float clouds = Fbm(d.x * 3.4f + 12f, d.z * 3.4f + 7f, 5) * 0.7f
                         + Fbm(d.y * 6.1f + 3f, d.x * 6.1f + 9f, 4) * 0.3f;

            float dust = Mathf.Pow(Fbm(d.x * 9.0f + 21f, d.z * 9.0f + 5f, 4), 2.4f);

            float value = band * Mathf.Clamp01(clouds * 1.35f) * (1f - dust * 0.78f) * 0.85f * MilkyWayBrightness;

            sky.r += value * 0.55f;
            sky.g += value * 0.60f;
            sky.b += value * 0.78f;
        }

        /// <summary>
        /// Радиус сплата звезды в долях угловой величины. Множитель 1.7 держит
        /// пятно в пределах одного текселя: при 2.2 край доходил до трёх.
        /// </summary>
        public const float StarSplatScale = 1.7f;

        /// <summary>
        /// Вклад одной звезды в точку направления <paramref name="d"/>.
        /// Возвращает линейный HDR. Сплатится аддитивно.
        /// </summary>
        public static Color StarContribution(in Star star, Vector3 d)
        {
            float cosAngle = Vector3.Dot(d, star.Direction);
            float cosEdge = Mathf.Cos(star.Radius * StarSplatScale);
            if (cosAngle < cosEdge)
                return new Color(0f, 0f, 0f, 1f);

            float a = (cosAngle - cosEdge) / Mathf.Max(1e-6f, 1f - cosEdge);
            float point = a * a * (3f - 2f * a);

            float e = 0f;
            if (star.Spike > 0f)
            {
                // Дифракционные лучи вдоль двух осей в плоскости, касательной
                // к направлению на звезду.
                Vector3 tangent = Vector3.Cross(star.Direction, Vector3.up);
                if (tangent.sqrMagnitude < 1e-5f) tangent = Vector3.right;
                tangent.Normalize();
                Vector3 bitangent = Vector3.Cross(star.Direction, tangent);

                float along = Mathf.Abs(Vector3.Dot(d, tangent));
                float across = Mathf.Abs(Vector3.Dot(d, bitangent));
                float len = star.Radius * 22f;

                float s1 = Mathf.Exp(-Mathf.Pow(across / (star.Radius * 0.9f), 2f)) * Mathf.Exp(-along / len);
                float s2 = Mathf.Exp(-Mathf.Pow(along / (star.Radius * 0.9f), 2f)) * Mathf.Exp(-across / len);
                e = (s1 + s2) * star.Spike;
            }

            float contribution = point + e * 0.5f;
            float lum = star.Luminance;
            return new Color(lum * star.Tint.r * contribution,
                             lum * star.Tint.g * contribution,
                             lum * star.Tint.b * contribution,
                             1f);
        }

        /// <summary>
        /// Фрактальный шум на зацикленной сетке. Зацикленность нужна кубомапе:
        /// соседние грани обязаны совпадать, иначе на рёбрах куба будут швы.
        /// </summary>
        public static float Fbm(float x, float y, int octaves)
        {
            float sum = 0f;
            float amplitude = 0.5f;
            float frequency = 1f;
            for (int o = 0; o < octaves; o++)
            {
                sum += ValueNoise(x * frequency, y * frequency) * amplitude;
                frequency *= 2.03f;
                amplitude *= 0.5f;
            }
            return sum / (1f - Mathf.Pow(0.5f, octaves));
        }

        public static float ValueNoise(float x, float y)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            int xi0 = Mod(x0, NoiseGrid);
            int xi1 = Mod(x0 + 1, NoiseGrid);
            int yi0 = Mod(y0, NoiseGrid);
            int yi1 = Mod(y0 + 1, NoiseGrid);

            float a = _noise[yi0 * NoiseGrid + xi0];
            float b = _noise[yi0 * NoiseGrid + xi1];
            float c = _noise[yi1 * NoiseGrid + xi0];
            float d = _noise[yi1 * NoiseGrid + xi1];

            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static int Mod(int value, int modulus)
        {
            int r = value % modulus;
            return r < 0 ? r + modulus : r;
        }

        /// <summary>
        /// Направление для пикселя грани кубомапы. Порядок граней у Unity:
        /// +X, -X, +Y, -Y, +Z, -Z. u растёт вправо, v — вниз.
        /// </summary>
        public static Vector3 FaceDirection(int face, float u, float v)
        {
            switch (face)
            {
                case 0: return new Vector3(1f, -v, -u);
                case 1: return new Vector3(-1f, -v, u);
                case 2: return new Vector3(u, 1f, v);
                case 3: return new Vector3(u, -1f, -v);
                case 4: return new Vector3(u, -v, 1f);
                default: return new Vector3(-u, -v, -1f);
            }
        }
    }
}
