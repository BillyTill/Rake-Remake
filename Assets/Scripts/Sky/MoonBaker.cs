using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lake.Sky
{
    /// <summary>
    /// Процедурная текстура Луны.
    ///
    /// Сделано не «шумом в белом круге», а настоящей моделью: карта высот из
    /// морей и кратеров, нормаль из конечных разностей, ламбертово затенение
    /// и лимбовое затемнение.
    ///
    /// Ключевое к реализму — контраст. Прошлая версия светила в 2500 нит при
    /// альбедо 0,5…0,75, и после экспозиции EV100 = 10 и тонмаппинга и тени, и
    /// света уходили в clipping: диск выходил идеально белым кругом. Настоящая
    /// Луна — это контраст: моря вдвое темнее наветов, а к краю всё гаснет.
    /// Поэтому яркость диска снижена до ~1100 нит, а альбедо разведено в
    /// диапазон 0,25…1,0.
    ///
    /// Отдельный билборд, а не часть кубомапы: настоящая Луна занимает 0.5°,
    /// и в кубомапе на грань 512 у неё выходит полторы точки — кратеров там
    /// быть не может физически.
    /// </summary>
    public static class MoonBaker
    {
        private const int NoiseGrid = 64;
        private static float[] _noise;
        private static bool _ready;

        /// <summary>Свет падает сверху-слева, как на большинстве снимков.</summary>
        private static readonly Vector2 LightDirection = new Vector2(-0.45f, 0.38f).normalized;

        public static void Init(int seed)
        {
            var random = new System.Random(seed);
            _noise = new float[NoiseGrid * NoiseGrid];
            for (int i = 0; i < _noise.Length; i++)
                _noise[i] = (float)random.NextDouble();
            _ready = true;
        }

        private static float Noise(float x, float y)
        {
            if (!_ready) Init(4242);

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

        private static float Fbm(float x, float y, int octaves)
        {
            float sum = 0f, amplitude = 0.5f, frequency = 1f;
            for (int o = 0; o < octaves; o++)
            {
                sum += Noise(x * frequency, y * frequency) * amplitude;
                frequency *= 2.07f;
                amplitude *= 0.5f;
            }
            return sum / (1f - Mathf.Pow(0.5f, octaves));
        }

        private static int Mod(int value, int modulus)
        {
            int r = value % modulus;
            return r < 0 ? r + modulus : r;
        }

        private struct Crater
        {
            public float X, Y, Radius, Depth;
        }

        private static Crater[] _craters;
        private static float[] _maria;

        /// <summary>
        /// Значение карты морей с билинейной интерполяцией. Без неё получаются
        /// жёсткие квадраты: соседние пиксели попадали в разные ячейки сетки
        /// и моря выглядели как пиксельный шум, а не как тёмные равнины.
        /// </summary>
        private static float MariaAt(float nx, float ny)
        {
            float fx = Mathf.Clamp((nx * 0.5f + 0.5f) * 63f, 0f, 63f);
            float fy = Mathf.Clamp((ny * 0.5f + 0.5f) * 63f, 0f, 63f);
            int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
            int x1 = Mathf.Min(x0 + 1, 63), y1 = Mathf.Min(y0 + 1, 63);
            float tx = fx - x0, ty = fy - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);

            float a = _maria[y0 * 64 + x0];
            float b = _maria[y0 * 64 + x1];
            float c = _maria[y1 * 64 + x0];
            float d = _maria[y1 * 64 + x1];
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        /// <summary>
        /// Поверхность Луны в точке (nx, ny) — точке на единичном диске.
        /// Возвращает высоту в относительных единицах: море — вниз, вал — вверх.
        /// </summary>
        private static float Height(float nx, float ny)
        {
            // Моря: крупные тёмные области с мягкими границами.
            float h = -SmoothStep(0.40f, 0.72f, MariaAt(nx, ny)) * 0.42f;

            // Мелкий рельеф.
            h += (Fbm(nx * 8f + 3f, ny * 8f + 7f, 5) - 0.5f) * 0.20f;

            // Кратеры: у каждого вал по краю и пониженное дно.
            for (int i = 0; i < _craters.Length; i++)
            {
                float dx = nx - _craters[i].X;
                float dy = ny - _craters[i].Y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float r = _craters[i].Radius;

                if (d > r * 1.35f)
                    continue;

                float t = d / r;
                float floorDepth = -0.85f * (1f - SmoothStep(0f, 0.82f, t));
                float rim = Mathf.Exp(-Mathf.Pow((t - 0.95f) / 0.22f, 2f)) * 0.95f;
                h += (floorDepth + rim) * _craters[i].Depth;
            }

            return h;
        }

        private static float SmoothStep(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / Mathf.Max(1e-5f, b - a));
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Текстура Луны. RGB — линейная яркость, A — покрытие.
        /// Диск занимает <paramref name="discRadius"/> в долях от центра текстуры.
        /// </summary>
        public static Texture2D Generate(int size, int seed, float discRadius)
        {
            var random = new System.Random(seed);
            Init(seed);

            // Сетка морей считается один раз на весь диск: иначе соседние
            // пиксели получили бы разные карты и появилась бы рябь.
            _maria = new float[64 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    _maria[y * 64 + x] = Fbm(x * 0.16f + 11f, y * 0.16f + 5f, 4);

            // Кратеры. Крупных единицы, мелких умеренно много: настоящая Луна
            // не «пузырчатая», большая часть поверхности — гладкие равнины.
            var list = new List<Crater>();
            for (int i = 0; i < 70; i++)
            {
                float angle = (float)(random.NextDouble() * Math.PI * 2.0);
                float radius = (float)(Math.Sqrt(random.NextDouble()) * 0.92);
                float craterSize = (float)(0.010 + 0.13 * Math.Pow(random.NextDouble(), 3.2));
                list.Add(new Crater
                {
                    X = (float)(Math.Cos(angle) * radius),
                    Y = (float)(Math.Sin(angle) * radius),
                    Radius = craterSize,
                    // Глубина растёт с радиусом: мелкие ямки почти плоские,
                    // крупные — глубокие. Иначе все кратеры выглядят одинаково.
                    Depth = 0.18f + craterSize * 2.6f + (float)(random.NextDouble() * 0.15)
                });
            }
            _craters = list.ToArray();

            var pixels = new Color[size * size];

            // Шаг сетки для конечных разностей: чем мельче, тем детальнее рельеф.
            float eps = 1.6f / size;

            for (int py = 0; py < size; py++)
            {
                float v = (py + 0.5f) / size * 2f - 1f;
                for (int px = 0; px < size; px++)
                {
                    float u = (px + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);

                    if (r > 0.98f)
                    {
                        pixels[py * size + px] = new Color(0f, 0f, 0f, 0f);
                        continue;
                    }

                    float alpha, radiance;

                    if (r <= discRadius)
                    {
                        float nx = u / discRadius;
                        float ny = v / discRadius;
                        float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));

                        float h = Height(nx, ny);

                        // Нормаль из конечных разностей карты высот.
                        float hx = Height(nx + eps, ny) - Height(nx - eps, ny);
                        float hy = Height(nx, ny + eps) - Height(nx, ny - eps);
                        float nx2 = -hx * 6f;
                        float ny2 = -hy * 6f;
                        float nz2 = 1f;
                        float inv = 1f / Mathf.Sqrt(nx2 * nx2 + ny2 * ny2 + nz2 * nz2);
                        nx2 *= inv; ny2 *= inv; nz2 *= inv;

                        // Ламберт. Свет слегка сбоку, иначе рельеф читается плоско.
                        float lambert = Mathf.Clamp01(nx2 * LightDirection.x + ny2 * LightDirection.y + nz2 * 0.80f);
                        lambert = 0.14f + 0.86f * lambert;

                        // Альбедо: моря темнее наветов примерно вдвое.
                        // Больше контраста не нужно — настоящие моря
                        // выглядят серыми пятнами, а не чёрными дырами.
                        float albedo = Mathf.Lerp(1.0f, 0.45f, SmoothStep(0.40f, 0.72f, MariaAt(nx, ny)));

                        // Лимбовое затемнение: у края диска мы видим срез
                        // поверхности под острым углом.
                        float edge = r / discRadius;
                        float limb = Mathf.Pow(Mathf.Clamp01(1f - edge * edge), 0.62f);

                        // Диск около 1100 нит: при EV100 = 10 это единица
                        // линейного значения, то есть яркие наветы уходят в
                        // clipping, а моря остаются различимыми.
                        radiance = 1100f * albedo * lambert * limb;

                        float soft = Mathf.Clamp01((discRadius - r) / (discRadius * 0.02f));
                        alpha = soft * soft * (3f - 2f * soft);
                    }
                    else
                    {
                        // Ореол. Квадратичное спадение — то мягкое гало, что
                        // видно вокруг настоящей Луны.
                        float g = (r - discRadius) / Mathf.Max(1e-4f, 1f - discRadius);
                        radiance = 40f / (1f + g * g * 150f) + 6f / (1f + g * g * 18f);
                        alpha = Mathf.Clamp01(1f - g * 1.6f);
                        alpha *= alpha;
                    }

                    pixels[py * size + px] = new Color(radiance * 0.98f, radiance * 0.97f, radiance * 0.92f, alpha);
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBAHalf, false, false);
            texture.name = "Moon";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }
    }
}
