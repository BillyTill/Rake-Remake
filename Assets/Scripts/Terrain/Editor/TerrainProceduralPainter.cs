using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Lake.TerrainTools
{
    /// <summary>
    /// Процедурная раскраска ландшафта.
    ///
    /// Правила:
    ///  * крутой склон — камень (Terrain_Layer8_Stones);
    ///  * низина у реки — песок (Terrain_Layer1_Sand): ниже уровня воды и до ~2 м выше;
    ///  * ровные места — смесь травяного покрова (Plants/Leaves/Moss/Roots) плюс detail-трава
    ///    из трёх видов префабов пака (grass_01/02/03), перемешанных по шуму.
    ///
    /// Все девять тайлов получают одинаковый набор слоёв в одинаковом порядке — иначе на стыках
    /// тайлов разные текстуры и «шахматка». Шум считается в мировых координатах, поэтому рисунок
    /// продолжается через границы тайлов.
    ///
    /// Меню: Tools/Lake/Terrain/Analyze, Tools/Lake/Terrain/Paint Procedurally.
    /// </summary>
    public static class TerrainProceduralPainter
    {
        const string LayerFolder = "Assets/NatureManufacture Assets/Forest Environment Dynamic Nature/Terrain Layers";
        const string PreviewPath = @"C:\Users\KOMPUHTER\AppData\Local\Temp\opencode\terrain_preview.png";
        const string LogPrefix = "[TerrainPaint]";

        /// <summary>Уровень воды, если объект River не найден.</summary>
        const float FallbackWaterLevel = 22f;

        const int MosaicTile = 512;

        // Порядок слоёв — общий для всех тайлов.
        static readonly string[] LayerNames =
        {
            "Terrain_Layer1_Sand",        // 0 песок
            "Terrain_Layer3_Soil_Wet",    // 1 мокрый грунт
            "Terrain_Layer2_Soil",        // 2 грунт
            "Terrain_Layer4_Grass_Plants",// 3 трава/растения
            "Terrain_Layer5_Leaves",      // 4 листва
            "Terrain_Layer6_Moss",        // 5 мох
            "Terrain_Layer7_Roots",       // 6 корни
            "Terrain_Layer8_Stones",      // 7 камни
        };

        static readonly Color[] LayerColors =
        {
            new Color(0.86f, 0.80f, 0.58f),
            new Color(0.34f, 0.29f, 0.21f),
            new Color(0.45f, 0.35f, 0.23f),
            new Color(0.36f, 0.52f, 0.20f),
            new Color(0.62f, 0.47f, 0.20f),
            new Color(0.24f, 0.44f, 0.19f),
            new Color(0.36f, 0.29f, 0.18f),
            new Color(0.52f, 0.52f, 0.54f),
        };

        static readonly string[] GrassPrefabNames =
        {
            "prefab_Unity_Terrain_grass_01_1",
            "prefab_Unity_Terrain_grass_02_2",
            "prefab_Unity_Terrain_grass_03_3",
        };

        // ------------------------------------------------------------------ меню

        [MenuItem("Tools/Lake/Terrain/Analyze")]
        public static void Analyze()
        {
            var terrains = FindTerrains();
            float waterY = FindWaterLevel();

            Log($"тайлов: {terrains.Length}, уровень воды: {waterY:0.00}");

            foreach (var terrain in terrains)
            {
                var td = terrain.terrainData;
                var heights = td.GetHeights(0, 0, td.heightmapResolution, td.heightmapResolution);

                float min = float.MaxValue, max = float.MinValue;
                int below = 0, steep = 0, flat = 0, total = 0;
                var slopeBuckets = new int[4];

                int step = Mathf.Max(1, heights.GetLength(0) / 128);
                float cellX = td.size.x / (td.heightmapResolution - 1);
                float cellZ = td.size.z / (td.heightmapResolution - 1);

                for (int z = 1; z < heights.GetLength(0) - 1; z += step)
                {
                    for (int x = 1; x < heights.GetLength(0) - 1; x += step)
                    {
                        float wy = terrain.transform.position.y + heights[z, x] * td.size.y;
                        min = Mathf.Min(min, wy);
                        max = Mathf.Max(max, wy);
                        total++;
                        if (wy < waterY) below++;

                        float gx = (heights[z, x + 1] - heights[z, x - 1]) * td.size.y / (2f * cellX);
                        float gz = (heights[z + 1, x] - heights[z - 1, x]) * td.size.y / (2f * cellZ);
                        float slope = Mathf.Atan(Mathf.Sqrt(gx * gx + gz * gz)) * Mathf.Rad2Deg;

                        if (slope > 32f) steep++;
                        if (slope < 12f) flat++;
                        slopeBuckets[Mathf.Min(3, (int)(slope / 15f))]++;
                    }
                }

                var layerNames = string.Join(", ", td.terrainLayers.Select(l => l != null ? l.name : "null"));
                Log($"{terrain.name} | слоёв {td.alphamapLayers} [{layerNames}] | H {min:0.0}..{max:0.0} | " +
                    $"под водой {100f * below / total:0.0}% | склон >32° {100f * steep / total:0.0}% | " +
                    $"ровно <12° {100f * flat / total:0.0}% | detail {td.detailWidth}x{td.detailHeight} прототипов {td.detailPrototypes.Length}");
            }

            Log($"префабы травы: {string.Join(", ", GrassPrefabNames.Select(n => $"{n}→{(FindPrefab(n) != null ? "ok" : "НЕТ")}"))}");
        }

        [MenuItem("Tools/Lake/Terrain/Paint Procedurally")]
        public static void Paint()
        {
            var sw = Stopwatch.StartNew();
            var terrains = FindTerrains();
            var layers = LoadLayers();
            if (layers == null) return;

            var grassPrefabs = GrassPrefabNames.Select(FindPrefab).ToArray();
            float waterY = FindWaterLevel();

            int painted = 0;
            var mosaic = new Color[MosaicTile * 3, MosaicTile * 3];
            float originX = terrains.Min(t => t.transform.position.x);
            float originZ = terrains.Min(t => t.transform.position.z);

            foreach (var terrain in terrains)
            {
                var td = terrain.terrainData;
                var splats = PaintSplats(terrain, layers, waterY, out var grassDensity);
                td.terrainLayers = layers;
                td.SetAlphamaps(0, 0, splats);

                if (grassPrefabs.All(p => p != null))
                    PaintDetailGrass(terrain, grassPrefabs, grassDensity);

                EditorUtility.SetDirty(td);
                painted++;

                // Мозаика для предпросмотра.
                int gx = Mathf.RoundToInt((terrain.transform.position.x - originX) / td.size.x);
                int gz = Mathf.RoundToInt((terrain.transform.position.z - originZ) / td.size.z);
                int res = td.alphamapResolution;
                for (int y = 0; y < MosaicTile; y++)
                {
                    int sy = Mathf.Clamp(Mathf.RoundToInt((y + 0.5f) / MosaicTile * res), 0, res - 1);
                    for (int x = 0; x < MosaicTile; x++)
                    {
                        int sx = Mathf.Clamp(Mathf.RoundToInt((x + 0.5f) / MosaicTile * res), 0, res - 1);
                        Color c = Color.black;
                        for (int i = 0; i < splats.GetLength(2); i++)
                            c += LayerColors[i] * splats[sy, sx, i];
                        int px = gx * MosaicTile + x;
                        int pz = (2 - gz) * MosaicTile + y; // +Z вверх
                        if (px >= 0 && px < mosaic.GetLength(1) && pz >= 0 && pz < mosaic.GetLength(0))
                            mosaic[pz, px] = c;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteMosaic(mosaic);

            Log($"готово: {painted} тайлов за {sw.Elapsed.TotalSeconds:0.0} c, предпросмотр: {PreviewPath}");
        }

        [MenuItem("Tools/Lake/Terrain/Preview Map")]
        public static void PreviewMap()
        {
            var terrains = FindTerrains();
            var mosaic = new Color[MosaicTile * 3, MosaicTile * 3];
            float originX = terrains.Min(t => t.transform.position.x);
            float originZ = terrains.Min(t => t.transform.position.z);

            foreach (var terrain in terrains)
            {
                var td = terrain.terrainData;
                int res = td.alphamapResolution;
                var splats = td.GetAlphamaps(0, 0, res, res);
                int gx = Mathf.RoundToInt((terrain.transform.position.x - originX) / td.size.x);
                int gz = Mathf.RoundToInt((terrain.transform.position.z - originZ) / td.size.z);

                for (int y = 0; y < MosaicTile; y++)
                {
                    int sy = Mathf.Clamp(Mathf.RoundToInt((y + 0.5f) / MosaicTile * res), 0, res - 1);
                    for (int x = 0; x < MosaicTile; x++)
                    {
                        int sx = Mathf.Clamp(Mathf.RoundToInt((x + 0.5f) / MosaicTile * res), 0, res - 1);
                        Color c = Color.black;
                        for (int i = 0; i < splats.GetLength(2); i++)
                            c += LayerColors[i] * splats[sy, sx, i];
                        int px = gx * MosaicTile + x;
                        int pz = (2 - gz) * MosaicTile + y;
                        if (px >= 0 && px < mosaic.GetLength(1) && pz >= 0 && pz < mosaic.GetLength(0))
                            mosaic[pz, px] = c;
                    }
                }
            }

            WriteMosaic(mosaic);
            Log($"предпросмотр обновлён: {PreviewPath}");
        }

        // --------------------------------------------------------------- рисование

        /// <summary>Сплаты по правилам «склон/река/трава».</summary>
        static float[,,] PaintSplats(Terrain terrain, TerrainLayer[] layers, float waterY, out float[,] grassDensity)
        {
            var td = terrain.terrainData;
            int hw = td.heightmapResolution;
            int aw = td.alphamapResolution;
            var heights = td.GetHeights(0, 0, hw, hw);

            Vector3 origin = terrain.transform.position;
            Vector3 size = td.size;
            float cellX = size.x / (hw - 1);
            float cellZ = size.z / (hw - 1);

            var splats = new float[aw, aw, layers.Length];
            grassDensity = new float[aw, aw];

            for (int z = 0; z < aw; z++)
            {
                float v = (z + 0.5f) / aw;
                int hz = Mathf.Clamp(Mathf.RoundToInt(v * (hw - 1)), 0, hw - 1);
                float wz = origin.z + v * size.z;

                for (int x = 0; x < aw; x++)
                {
                    float u = (x + 0.5f) / aw;
                    int hx = Mathf.Clamp(Mathf.RoundToInt(u * (hw - 1)), 0, hw - 1);
                    float wx = origin.x + u * size.x;

                    float wy = origin.y + heights[hz, hx] * size.y;

                    float hl = heights[hz, Mathf.Max(hx - 1, 0)];
                    float hr = heights[hz, Mathf.Min(hx + 1, hw - 1)];
                    float hd = heights[Mathf.Max(hz - 1, 0), hx];
                    float hu = heights[Mathf.Min(hz + 1, hw - 1), hx];
                    float gx = (hr - hl) * size.y / (2f * cellX);
                    float gz = (hu - hd) * size.y / (2f * cellZ);
                    float slope = Mathf.Atan(Mathf.Sqrt(gx * gx + gz * gz)) * Mathf.Rad2Deg;

                    // --- шум в мировых координатах: рисунок не рвётся на стыках тайлов
                    float nRock = Fbm(wx * 0.011f + 7.31f, wz * 0.011f + 2.17f, 3);
                    float nEdge = Fbm(wx * 0.061f + 3.73f, wz * 0.061f + 8.91f, 3);
                    float nPatch = Fbm(wx * 0.0131f + 21.1f, wz * 0.0131f + 5.53f, 4);
                    float nFine = Fbm(wx * 0.093f + 41.7f, wz * 0.093f + 13.3f, 2);

                    float above = wy - waterY;

                    // --- 1. песок: ниже воды и ~2 м выше, кромка гуляет по шуму
                    float sandTop = 2.0f + (nEdge - 0.5f) * 0.7f;
                    float sand = above < sandTop ? 1f : Mathf.Clamp01(1f - (above - sandTop) / 1.2f);
                    sand = Mathf.SmoothStep(0f, 1f, sand);
                    sand *= Mathf.Clamp01(1f - (slope - 28f) / 14f); // на обрывах песок не держится

                    // --- 2. мокрый грунт на урезе воды
                    float wet = Mathf.Clamp01(1f - Mathf.Abs(above - 0.9f) / 1.9f);
                    wet *= 0.5f + 0.5f * nEdge;
                    wet *= Mathf.Clamp01(1f - (slope - 18f) / 14f);

                    // --- 3. камень на крутых склонах (порог гуляет по шуму)
                    float rock = Mathf.SmoothStep(0f, 1f, (slope - (32f + (nRock - 0.5f) * 10f)) / 12f);

                    // --- 4. грунт: переходные склоны + проплешины
                    float soil = Mathf.SmoothStep(0f, 1f, (slope - 14f) / 12f) * (1f - rock);
                    soil *= 0.85f + 0.15f * nPatch;
                    soil = Mathf.Max(soil, Mathf.Clamp01((nFine - 0.62f) / 0.12f) * 0.35f * (1f - sand) * (1f - rock));

                    // --- 5. травяной покров: всё, что осталось
                    float cover = Mathf.Clamp01(1f - Mathf.Max(Mathf.Max(sand, wet * 0.8f), Mathf.Max(rock, soil)));

                    float wPlants = Mathf.Lerp(0.35f, 1.0f, nPatch);
                    float wMoss = Mathf.Lerp(0.20f, 0.85f, Fbm(wx * 0.020f + 91.3f, wz * 0.020f + 17.7f, 3));
                    wMoss *= Mathf.Lerp(1f, 0.45f, Mathf.Clamp01((above - 3f) / 25f));
                    float wLeaves = Mathf.Lerp(0.15f, 0.75f, Fbm(wx * 0.024f + 55.9f, wz * 0.024f + 71.1f, 3));
                    float wRoots = Mathf.Clamp01((nFine - 0.73f) / 0.15f) * 0.45f;

                    float wSum = wPlants + wMoss + wLeaves + wRoots;
                    if (wSum < 1e-4f) { wPlants = 1f; wSum = 1f; }

                    float plants = cover * wPlants / wSum;
                    float moss = cover * wMoss / wSum;
                    float leaves = cover * wLeaves / wSum;
                    float roots = cover * wRoots / wSum;

                    float total = sand + wet + soil + rock + plants + moss + leaves + roots;
                    if (total < 1e-4f) { plants = 1f; total = 1f; }
                    float inv = 1f / total;

                    splats[z, x, 0] = sand * inv;
                    splats[z, x, 1] = wet * inv;
                    splats[z, x, 2] = soil * inv;
                    splats[z, x, 3] = plants * inv;
                    splats[z, x, 4] = leaves * inv;
                    splats[z, x, 5] = moss * inv;
                    splats[z, x, 6] = roots * inv;
                    splats[z, x, 7] = rock * inv;

                    // плотность detail-травы: только на «зелёном» и не на склоне
                    float grassMask = Mathf.Clamp01(plants + leaves * 0.5f + moss * 0.6f);
                    grassMask *= Mathf.Clamp01(1f - (slope - 20f) / 10f);
                    grassMask *= Mathf.Clamp01((above - sandTop - 0.3f) / 1.5f);
                    grassDensity[z, x] = grassMask;
                }
            }

            return splats;
        }

        /// <summary>Три вида detail-травы, перемешанные по шуму.</summary>
        static void PaintDetailGrass(Terrain terrain, GameObject[] prefabs, float[,] grassDensity)
        {
            var td = terrain.terrainData;
            int dw = td.detailWidth;
            int dh = td.detailHeight;
            int aw = td.alphamapResolution;

            var prototypes = new DetailPrototype[prefabs.Length];
            for (int i = 0; i < prefabs.Length; i++)
            {
                prototypes[i] = new DetailPrototype
                {
                    usePrototypeMesh = true,
                    prototype = prefabs[i],
                    minWidth = 0.65f,
                    maxWidth = 1.4f,
                    minHeight = 0.65f,
                    maxHeight = 1.4f,
                    noiseSeed = 17 + i * 31,
                    noiseSpread = 0.35f,
                    healthyColor = new Color(0.72f, 0.80f, 0.55f),
                    dryColor = new Color(0.58f, 0.57f, 0.38f),
                    renderMode = DetailRenderMode.VertexLit,
                    useInstancing = true,
                };
            }

            td.detailPrototypes = prototypes;
            td.RefreshPrototypes();

            var species = new[]
            {
                new float[dh, dw],
                new float[dh, dw],
                new float[dh, dw],
            };

            for (int z = 0; z < dh; z++)
            {
                int az = Mathf.Clamp(Mathf.RoundToInt((z + 0.5f) / dh * aw), 0, aw - 1);
                for (int x = 0; x < dw; x++)
                {
                    int ax = Mathf.Clamp(Mathf.RoundToInt((x + 0.5f) / dw * aw), 0, aw - 1);
                    float mask = grassDensity[az, ax];
                    if (mask <= 0.01f) continue;

                    for (int i = 0; i < species.Length; i++)
                    {
                        // разные виды травы любят разные пятна
                        float n = Fbm(x * 0.021f + i * 53.1f, z * 0.021f + i * 29.7f, 2);
                        float density = mask * (i == 0 ? 2.2f : 1.6f) * Mathf.Lerp(0.2f, 1.4f, n);
                        species[i][z, x] = Mathf.Clamp(Mathf.Round(density), 0f, 4f);
                    }
                }
            }

            for (int i = 0; i < species.Length; i++)
            {
                var ints = new int[dh, dw];
                for (int z = 0; z < dh; z++)
                    for (int x = 0; x < dw; x++)
                        ints[z, x] = (int)species[i][z, x];
                td.SetDetailLayer(0, 0, i, ints);
            }
        }

        // ------------------------------------------------------------------ утилиты

        static Terrain[] FindTerrains()
        {
            return UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None)
                .Where(t => t.isActiveAndEnabled && t.terrainData != null)
                .OrderBy(t => t.transform.position.z)
                .ThenBy(t => t.transform.position.x)
                .ToArray();
        }

        static TerrainLayer[] LoadLayers()
        {
            var result = new TerrainLayer[LayerNames.Length];
            for (int i = 0; i < LayerNames.Length; i++)
            {
                string path = $"{LayerFolder}/{LayerNames[i]}.terrainlayer";
                result[i] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                if (result[i] == null)
                {
                    Debug.LogError($"{LogPrefix} не найден слой {path}");
                    return null;
                }
            }
            return result;
        }

        static GameObject FindPrefab(string name)
        {
            var guid = AssetDatabase.FindAssets($"t:Prefab {name}").FirstOrDefault();
            return string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
        }

        static float FindWaterLevel()
        {
            var river = GameObject.Find("River");
            if (river != null)
                return river.transform.position.y;
            Debug.LogWarning($"{LogPrefix} объект River не найден, уровень воды = {FallbackWaterLevel}");
            return FallbackWaterLevel;
        }

        static float Fbm(float x, float y, int octaves)
        {
            float sum = 0f, amp = 0.5f, freq = 1f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * Mathf.PerlinNoise(x * freq, y * freq);
                freq *= 2.03f;
                amp *= 0.5f;
            }
            return Mathf.Clamp01(sum);
        }

        static void WriteMosaic(Color[,] mosaic)
        {
            int w = mosaic.GetLength(1);
            int h = mosaic.GetLength(0);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = mosaic[y, x];

            // нижняя строка мозаики — юг, в PNG переворачиваем
            var flipped = new Color[w * h];
            for (int y = 0; y < h; y++)
                Array.Copy(pixels, y * w, flipped, (h - 1 - y) * w, w);

            tex.SetPixels(flipped);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(PreviewPath) ?? ".");
            File.WriteAllBytes(PreviewPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        static void Log(string message) => Debug.Log($"{LogPrefix} {message}");
    }
}
