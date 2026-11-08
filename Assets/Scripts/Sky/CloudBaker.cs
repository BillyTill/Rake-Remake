using System;
using UnityEngine;

namespace Lake.Sky
{
    /// <summary>
    /// Процедурная текстура облаков для небесного купола.
    ///
    /// Ключевое требование — БЕСШОВНОСТЬ. Купол крутится и текстура скроллится,
    /// поэтому шум обязан быть периодическим по обеим осям: иначе на краях
    /// появятся вертикальные и горизонтальные швы, которые при движении
    /// превращаются в ползущие полосы.
    ///
    /// Обычный value noise с хешем по координате для этого не годится — нужен
    /// хеш с обёрткой по размеру сетки: соседние копии текстуры стыкуются.
    ///
    /// Форма — кучево-слоистые «рваные» сгустки, а не мягкие пятна: у ночных
    /// облаков резкие края, подсвеченные луной снизу.
    /// </summary>
    public static class CloudBaker
    {
        /// <summary>
        /// Текстура облаков. RGB — цвет, A — плотность (прозрачность в альбе).
        /// Генерируется в линейном HDR: ночные облака очень тёмные, и при
        /// экспозиции проекта EV100 = 10 обычный белый цвет ушёл бы в ноль.
        /// </summary>
        /// <param name="size">Размер текстуры, степень двойки.</param>
        /// <param name="seed">Seed детерминированной генерации.</param>
        /// <param name="coverage">Доля покрытия неба, 0…1.</param>
        /// <param name="turbidity">Резкость краёв сгустков. Больше — рванее.</param>
        public static Texture2D Generate(int size, int seed, float coverage, float turbidity)
        {
            var random = new System.Random(seed);
            const int grid = 8;
            var lattice = new float[grid * grid];
            for (int i = 0; i < lattice.Length; i++)
                lattice[i] = (float)random.NextDouble();

            // Смещение для второго октава: отдельная решётка со своим масштабом,
            // но той же периодичностью.
            const int grid2 = 16;
            var lattice2 = new float[grid2 * grid2];
            for (int i = 0; i < lattice2.Length; i++)
                lattice2[i] = (float)random.NextDouble();

            var pixels = new Color[size * size];

            for (int py = 0; py < size; py++)
            {
                float v = py / (float)size;
                for (int px = 0; px < size; px++)
                {
                    float u = px / (float)size;

                    float n = 0f;
                    n += PeriodicFbm(lattice, grid, u, v, 4) * 0.62f;
                    n += PeriodicFbm(lattice2, grid2, u + 0.37f, v + 0.11f, 3) * 0.38f;

                    // Порог и гладкая кромка: у облаков есть плотное ядро и
                    // размытая граница, а не резкая нарезка по порогу.
                    float edge = Mathf.Lerp(0.30f, 0.02f, Mathf.Clamp01(turbidity));
                    float d = Mathf.Clamp01((n - (1f - coverage)) / Mathf.Max(1e-4f, edge));

                    // Плотность ядра. Кубическая даёт «пухлое» облако.
                    float density = d * d * (3f - 2f * d);

                    // Вертикальное освещение: ночью облака подсвечены снизу
                    // луной и лежат в тени сверху. Без этого они выглядят
                    // плоским серым пятном.
                    float height = Mathf.Clamp01((n - 0.25f) / 0.75f);
                    float underlit = Mathf.Lerp(0.35f, 1.0f, Mathf.Pow(1f - height, 1.6f));

                    // Ночная яркость: около 1.5 нит на плотном облаке.
                    // При EV100 = 10 это 0.0015 линейного значения — почти
                    // чёрный, но различимый на фоне неба в 0.01.
                    const float nightCloud = 1.6f;
                    float radiance = density * nightCloud * underlit;

                    // Холодный оттенок от луны, тёплый пыльный от городов внизу.
                    float warmth = Mathf.Clamp01((1f - height) * 0.8f);
                    var col = new Color(
                        radiance * Mathf.Lerp(0.82f, 0.95f, warmth),
                        radiance * Mathf.Lerp(0.88f, 0.90f, warmth),
                        radiance * 1.00f,
                        density);

                    pixels[py * size + px] = col;
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBAHalf, false, false);
            texture.name = "NightClouds";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 4;
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static float PeriodicFbm(float[] lattice, int grid, float u, float v, int octaves)
        {
            float sum = 0f;
            float amplitude = 0.5f;
            float frequency = 1f;
            for (int o = 0; o < octaves; o++)
            {
                sum += PeriodicNoise(lattice, grid, u * frequency, v * frequency) * amplitude;
                frequency *= 2f;
                amplitude *= 0.5f;
            }
            return sum / (1f - Mathf.Pow(0.5f, octaves));
        }

        /// <summary>
        /// Value noise с обёрткой по размеру решётки. Обёртка и делает
        /// текстуру бесшовной: точка на краю и точка на 0 — одна и та же ячейка.
        /// </summary>
        private static float PeriodicNoise(float[] lattice, int grid, float x, float y)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            int xi0 = Mod(x0, grid), xi1 = Mod(x0 + 1, grid);
            int yi0 = Mod(y0, grid), yi1 = Mod(y0 + 1, grid);

            float a = lattice[yi0 * grid + xi0];
            float b = lattice[yi0 * grid + xi1];
            float c = lattice[yi1 * grid + xi0];
            float d = lattice[yi1 * grid + xi1];

            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static int Mod(int value, int modulus)
        {
            int r = value % modulus;
            return r < 0 ? r + modulus : r;
        }
    }
}
