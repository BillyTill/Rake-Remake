using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using UnityEngine.Rendering.HighDefinition;

namespace Lake.Vegetation.EditorTools
{
    /// <summary>
    /// Пайплайн билбордов растений:
    ///
    ///  * Bake Plant Photos — «фотографирует» все префабы растений пака
    ///    (камера на пурпурном фоне, снимок 256x256), хромакей убирает фон
    ///    в альфу, результат кладётся в Assets/LakeBillboards как PNG,
    ///    база PlantBillboardDatabase.asset заполняется записями;
    ///  * Ensure Manager In Scene — создаёт/обновляет PlantBillboardManager
    ///    в активной сцене (с автосканом растений и свопом на последний LOD);
    ///  * Plant Demo Cluster / Remove Demo Plants — тестовая рассадка рядом
    ///    со спавном, чтобы увидеть своп на билборд (можно удалять).
    /// </summary>
    public static class PlantBillboardBuilder
    {
        const string OutDir = "Assets/LakeBillboards";
        const string DbPath = OutDir + "/PlantBillboardDatabase.asset";
        const string LogPrefix = "[Billboards]";
        const string PhotoLayerName = "PhotoBake";
        const string DemoMarker = "LakePlantDemoPlants";

        static readonly string[] PlantFolders =
        {
            "Assets/NatureManufacture Assets/Forest Environment Dynamic Nature/Beech Trees/Prefabs",
            "Assets/NatureManufacture Assets/Forest Environment Dynamic Nature/Bushes/Prefabs",
            "Assets/NatureManufacture Assets/Forest Environment Dynamic Nature/Foliage and Grass/Prefabs",
            "Assets/NatureManufacture Assets/Forest Environment Dynamic Nature/Foliage and Grass/Unity Terrain Prefabs",
            "Assets/NatureManufacture Assets/Forest Environment Dynamic Nature/Mushrooms/Prefabs",
            "Assets/NatureManufacture Assets/Forest Environment Dynamic Nature/Details/Prefabs",
        };

        const int PhotoSize = 256;
        internal const string BakeVersion = "8.6"; // fix: материалы билборда HDRP/Unlit → HDRP/Lit (реакция на свет сцены)
        static readonly Vector3 PhotoFramingGap = new Vector3(1.15f, 1.15f, 1f); // рамка вокруг растения

        // ------------------------------------------------------------------ меню

        [MenuItem("Tools/Lake/Vegetation/Bake Plant Photos")]
        public static void BakeAll()
        {
            var trace = new System.Text.StringBuilder();
            trace.AppendLine("BAKE v" + BakeVersion + " start");

            Directory.CreateDirectory(OutDir);

            int layer = EnsureLayer(PhotoLayerName);
            trace.AppendLine("layer=" + layer);
            if (layer < 0)
            {
                Debug.LogError($"{LogPrefix} нет свободного слоя для PhotoBake");
                trace.AppendLine("FAIL no layer");
                File.WriteAllText(OutDir + "/bake_trace.txt", trace.ToString());
                return;
            }

            if (!AssetDatabase.LoadAssetAtPath<PlantBillboardDatabase>(DbPath))
                CreateDatabase(DbPath);
            var db = AssetDatabase.LoadAssetAtPath<PlantBillboardDatabase>(DbPath);
            db.entries.Clear();

            string skyLog;
            int total = 0, failed = 0;
            RenderTexture rt = null;
            try
            {
                KillSky(out skyLog);
                trace.AppendLine(skyLog);

                rt = new RenderTexture(PhotoSize, PhotoSize, 24, RenderTextureFormat.ARGBHalf);

                foreach (var folder in PlantFolders)
                {
                    var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
                    foreach (var g in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(g);
                        string stem = Path.GetFileNameWithoutExtension(path);
                        total++;
                        if (EditorUtility.DisplayCancelableProgressBar("Lake billboards", stem, total % 97 / 97f))
                            break;

                        try
                        {
                            if (BakeOne(path, stem, rt, layer, out var entry, db))
                            {
                                db.entries.Add(entry);
                            }
                            else
                            {
                                failed++;
                                trace.AppendLine("SKIP " + stem);
                            }
                        }
                        catch (System.Exception e)
                        {
                            failed++;
                            Debug.LogError($"{LogPrefix} {stem}: {e.Message}");
                            trace.AppendLine("ERR " + stem + ": " + e.Message);
                        }
                    }
                }
            }
            finally
            {
                if (rt != null) rt.Release();
                RestoreSky();
                EditorUtility.ClearProgressBar();
                trace.AppendLine("done: photos=" + db.entries.Count + " total=" + total + " failed=" + failed);
                File.WriteAllText(OutDir + "/bake_trace.txt", trace.ToString());
            }

            db.RebuildMap();
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();

            Debug.Log($"{LogPrefix} bake готов: {db.entries.Count} фото в {OutDir}, ошибок {failed}");
        }

        [MenuItem("Tools/Lake/Vegetation/Ensure Manager In Scene")]
        public static void EnsureManager()
        {
            const string name = "LakePlantBillboards";
            GameObject holder = GameObject.Find(name);
            if (holder == null)
            {
                holder = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(holder, "Create PlantBillboardManager");
            }
            var manager = holder.GetComponent<PlantBillboardManager>();
            if (manager == null) manager = Undo.AddComponent<PlantBillboardManager>(holder);

            var db = AssetDatabase.LoadAssetAtPath<PlantBillboardDatabase>(DbPath);
            if (db == null)
            {
                Debug.LogError($"{LogPrefix} база нет — сначала Bake Plant Photos");
                return;
            }
            manager.database = db;
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(holder.scene);
            Debug.Log($"{LogPrefix} менеджер готов: {name}, записей в базе {db.entries.Count}");
        }

        [MenuItem("Tools/Lake/Vegetation/Plant Demo Cluster")]
        public static void PlantDemo()
        {
            var db = AssetDatabase.LoadAssetAtPath<PlantBillboardDatabase>(DbPath);
            if (db == null || db.entries.Count == 0)
            {
                Debug.LogError($"{LogPrefix} база пуста — сначала Bake Plant Photos");
                return;
            }
            db.RebuildMap();

            var spawn = GameObject.Find("PlayerSpawn");
            if (spawn == null) { Debug.LogError($"{LogPrefix} нет PlayerSpawn"); return; }

            var marker = GameObject.Find(DemoMarker);
            if (marker == null)
            {
                marker = new GameObject(DemoMarker);
                Undo.RegisterCreatedObjectUndo(marker, "Plant demo plants");
            }

            // выбираем часть префабов, характерных по размеру
            var pick = new List<PlantBillboardEntry>();
            foreach (var e in db.entries)
                if (e.sizeY >= 0.4f && e.sizeY <= 14f) pick.Add(e);
            pick.Sort((a, b) => string.CompareOrdinal(a.prefabName, b.prefabName));

            const int count = 48;
            for (int i = 0; i < count && pick.Count > 0; i++)
            {
                var entry = pick[Random.Range(0, pick.Count)];
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(14f, 55f);
                var pos = spawn.transform.position + new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);
                pos.y = Terrain.activeTerrains.Length > 0 ? Terrain.activeTerrains[0].SampleHeight(pos) + Terrain.activeTerrains[0].transform.position.y : pos.y;

                var prefab = FindPrefab(entry.prefabName);
                if (prefab == null) continue;

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(inst, "Plant demo plant");
                inst.transform.SetParent(marker.transform, true);
                inst.transform.position = pos;
                inst.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            Debug.Log($"{LogPrefix} демо-рассадка готова (посмотреть своп — DebugForceAll на {DemoMarker})");
        }

        [MenuItem("Tools/Lake/Vegetation/Remove Demo Plants")]
        public static void RemoveDemo()
        {
            var marker = GameObject.Find(DemoMarker);
            if (marker != null) { Undo.DestroyObjectImmediate(marker); Debug.Log($"{LogPrefix} демо удалено"); }
            else Debug.Log($"{LogPrefix} демо-рассадки нет");
        }

        // --------------------------------------------------- вшивание билборда в префаб

        /// <summary>
        /// Для каждого префаба с фотографией в базе:
        ///  * создаёт материал-ассет в Assets/LakeBillboards/Materials;
        ///  * добавляет в корень квады с фото (для высоких — крест из двух);
        ///  * добавляет квад(ы) ПОСЛЕДНИМ уровнем LODGroup — переключением
        ///    дальше управляет сама Unity, PlantBillboardManager такие растения
        ///    пропускает (детект по квадам BB_*).
        /// Идемпотентно: перед вшиванием старые билборды сносятся.
        /// </summary>
        [MenuItem("Tools/Lake/Vegetation/Add Billboard LOD To Prefabs")]
        public static void AddBillboardLOD()
        {
            var db = AssetDatabase.LoadAssetAtPath<PlantBillboardDatabase>(DbPath);
            if (db == null || db.entries.Count == 0)
            {
                Debug.LogError($"{LogPrefix} база пуста — сначала Bake Plant Photos");
                return;
            }
            db.RebuildMap();
            Directory.CreateDirectory(OutDir + "/Materials");

            var trace = new System.Text.StringBuilder();
            trace.AppendLine("ADD-LOD v" + BakeVersion + " start");

            int done = 0, failed = 0, total = 0;
            foreach (var folder in PlantFolders)
            {
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
                foreach (var g in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    string stem = Path.GetFileNameWithoutExtension(path);
                    total++;
                    var entry = db.Get(stem);
                    if (entry == null || entry.photo == null)
                    {
                        trace.AppendLine("SKIP " + stem + " (no photo)");
                        continue;
                    }
                    if (EditorUtility.DisplayCancelableProgressBar("Lake billboards", stem, total % 127 / 127f))
                        break;
                    try
                    {
                        ApplyBillboardLOD(path, stem, entry);
                        done++;
                    }
                    catch (System.Exception e)
                    {
                        failed++;
                        Debug.LogError($"{LogPrefix} {stem}: {e.Message}");
                        trace.AppendLine("ERR " + stem + ": " + e.Message);
                    }
                }
            }
            EditorUtility.ClearProgressBar();

            AssetDatabase.SaveAssets();
            trace.AppendLine($"done: added={done} of {total} failed={failed}");
            File.WriteAllText(OutDir + "/lod_trace.txt", trace.ToString());
            Debug.Log($"{LogPrefix} билборд-LOD вшит в префабы: {done} из {total}, ошибок {failed}");
        }

        /// <summary>Сносит вшитые билборды из префабов (квады BB_* + уровни LODGroup с ними).</summary>
        [MenuItem("Tools/Lake/Vegetation/Remove Billboard LOD From Prefabs")]
        public static void RemoveBillboardLOD()
        {
            int removed = 0, total = 0;
            foreach (var folder in PlantFolders)
            {
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
                foreach (var g in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    total++;
                    var root = PrefabUtility.LoadPrefabContents(path);
                    bool changed;
                    try
                    {
                        changed = StripBillboardLOD(root);
                        if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                    if (changed) removed++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"{LogPrefix} билборд-LOD снесён в {removed} из {total} префабов");
        }

        /// <summary>
        /// Материал билборда — ПЕРСИСТЕНТНЫЙ ассет (префаб не может ссылаться на материал Runtime).
        /// HDRP/Lit, а НЕ Unlit: unlit рисует фото «как есть» и не реагирует на свет
        /// сцены — в сумерках/ночью квадраты светятся на фоне затемнённой
        /// растительности. Lit затеняет квад вместе со сценой.
        /// Материал обновляется НА МЕСТЕ (GUID сохранить — на него ссылаются префабы).
        /// </summary>
        static Material LoadOrCreateMaterial(string stem, Texture2D photo)
        {
            string path = $"{OutDir}/Materials/{stem}_BB.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("HDRP/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            ApplyBillboardMaterial(mat, photo);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Общие настройки билборд-материала (тот же смысл у EnsureMaterial в менеджере).</summary>
        static void ApplyBillboardMaterial(Material mat, Texture2D photo)
        {
            mat.shader = Shader.Find("HDRP/Lit");
            mat.SetTexture("_BaseColorMap", photo);
            // «0.85» — лёгкая компенсация двойного затенения: фото уже снято
            // при кей-свете, а Lit досвечивает квад светом сцены
            mat.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.85f, 1f));
            // alpha-клип: прозрачность по фотографии, поверхность непрозрачная.
            // ВАЖНО: клип управляется КЛЮЧЕВЫМ СЛОВОМ _ALPHATEST_ON — без кея
            // рендерится чёрный прямоугольник (альфа текстуры не отсекается)
            mat.SetFloat("_SurfaceType", 0f);              // Opaque
            mat.SetFloat("_AlphaCutoffEnable", 1f);
            mat.SetFloat("_AlphaCutoff", 0.45f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetFloat("_ZWrite", 1f);
            mat.SetFloat("_BlendMode", 0f);
            mat.SetFloat("_EnableFogOnTransparent", 0f);
            // без бликов и металла: заметный блик на кваде выдаёт подмену
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0f);
            // двусторонний рендер: квад в LOD виден с любой стороны без поворота к камере
            mat.SetFloat("_CullMode", (float)CullMode.Off);
            mat.SetFloat("_OpaqueCullMode", (float)CullMode.Off);
            // билборд не тень и не трекер скорости
            mat.SetShaderPassEnabled("ShadowCaster", false);
            mat.SetShaderPassEnabled("MotionVectors", false);
            mat.renderQueue = 2000;
        }

        static bool ApplyBillboardLOD(string path, string stem, PlantBillboardEntry entry)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            try
            {
                StripBillboardLOD(root); // снос старого — повторный запуск идемпотентен

                var mat = LoadOrCreateMaterial(stem, entry.photo);

                // высокие растения — крест из двух квадов (паки делают так же
                // на своём последнем LOD): сбоку/сзади квад не виден ребром.
                // Мелочь (<1 м) — один квад.
                bool cross = entry.sizeY >= 1.0f;
                var yaws = cross ? new[] { 0, 90 } : new[] { 0 };
                var bbRends = new List<Renderer>(yaws.Length);
                foreach (var yaw in yaws)
                {
                    var q = new GameObject("BB_" + yaw);
                    q.transform.SetParent(root.transform, false);
                    q.layer = root.layer;

                    var mf = q.AddComponent<MeshFilter>();
                    mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                    var mr = q.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = mat;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                    mr.lightProbeUsage = LightProbeUsage.Off;

                    // offsets/size хранятся в ЛОКАЛЬНОМ фрейме корня (сняты
                    // через InverseTransformPoint при беке) — наследование
                    // scale корня само даёт правильный мировой размер
                    q.transform.localScale = new Vector3(entry.sizeX, entry.sizeY, 1f);
                    q.transform.localPosition = new Vector3(entry.offsetX, entry.offsetY, entry.offsetZ);
                    q.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                    bbRends.Add(mr);
                }

                var lod = root.GetComponentInChildren<LODGroup>(true);
                if (lod == null)
                {
                    // растения без LODGroup: двухуровневая группа — меши до порога,
                    // дальше билборд, глубже полный cull. Порог от размера: любой
                    // размер включается примерно на одинаковом расстоянии (~15 м)
                    var g = root.AddComponent<LODGroup>();
                    var orig = new List<Renderer>();
                    foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                        if (r != null && !r.name.StartsWith("BB_", StringComparison.Ordinal))
                            orig.Add(r);
                    float t0 = Mathf.Clamp(entry.sizeY / 20f, 0.02f, 0.10f);
                    g.SetLODs(new[]
                    {
                        new LOD(t0, orig.ToArray()),
                        new LOD(t0 * 0.15f, bbRends.ToArray()),
                    });
                    lod = g;
                }
                else
                {
                    var lods = lod.GetLODs();
                    float last = lods[lods.Length - 1].screenRelativeTransitionHeight;
                    float newLast = Mathf.Max(0.0004f, Mathf.Min(last * 0.5f, 0.06f));
                    if (newLast >= last) newLast = Mathf.Max(0.0001f, last * 0.5f);
                    var build = new LOD[lods.Length + 1];
                    for (int i = 0; i < lods.Length; i++) build[i] = lods[i];
                    build[lods.Length] = new LOD(newLast, bbRends.ToArray());
                    lod.SetLODs(build);
                }
                lod.RecalculateBounds();
                changed = true;
            }
            finally
            {
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            return changed;
        }

        /// <summary>
        /// Сносит билборды из содержимого префаба: детские квадры BB_* и уровни
        /// LODGroup, оставшиеся пустыми после удаления ссылок на них.
        /// </summary>
        static bool StripBillboardLOD(GameObject root)
        {
            bool removed = false;

            var doomed = new List<GameObject>();
            foreach (Transform c in root.transform)
                if (c.name.StartsWith("BB_", StringComparison.Ordinal) && c.GetComponent<MeshRenderer>() != null)
                    doomed.Add(c.gameObject);
            if (doomed.Count > 0) removed = true;
            foreach (var d in doomed) Object.DestroyImmediate(d);

            foreach (var lg in root.GetComponentsInChildren<LODGroup>(true))
            {
                var lods = lg.GetLODs();
                var build = new List<LOD>(lods.Length);
                bool dirty = false;
                for (int i = 0; i < lods.Length; i++)
                {
                    // после удаления квадов ссылки на них стали null — фильтруем
                    var rends = new List<Renderer>(lods[i].renderers.Length);
                    foreach (var r in lods[i].renderers)
                        if (r != null) rends.Add(r);

                    bool keep = i == 0 || rends.Count > 0;
                    if (keep) build.Add(new LOD(lods[i].screenRelativeTransitionHeight, rends.ToArray()));
                    else dirty = true;
                }
                if (!dirty) continue;
                removed = true;
                if (build.Count > 0)
                {
                    lg.SetLODs(build.ToArray());
                    lg.RecalculateBounds();
                }
                else
                {
                    Object.DestroyImmediate(lg);
                }
            }
            return removed;
        }

        // ------------------------------------------------------------------ бек

        static Volume _skyVol;
        static VolumeProfile _savedSkyProfile;
        static VolumeProfile _skyKillClone;

        /// <summary>
        /// На время бека временно подменяет профиль глобального sky/fog объёма
        /// (обычно «Sky and Fog Volume») клоном без тумана, объёмных облаков и
        /// блума — иначе они заливают фон фото. По окончании профиль
        /// восстанавливается.
        /// </summary>
        static void KillSky(out string log)
        {
            _skyVol = null; _savedSkyProfile = null; _skyKillClone = null;
            log = "no sky volume found";

            var sgObj = GameObject.Find("Sky and Fog Volume");
            Volume v = sgObj != null ? sgObj.GetComponent<Volume>() : null;
            if (v == null)
            {
                var vols = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
                foreach (var x in vols)
                    if (x.isGlobal && x.profile != null) { v = x; break; }
            }
            if (v == null || v.profile == null) return;

            _skyVol = v;
            _savedSkyProfile = v.profile;
            _skyKillClone = ScriptableObject.Instantiate(_savedSkyProfile);
            int killed = 0;
            foreach (var c in _skyKillClone.components)
            {
                if (c is Fog || c is VolumetricClouds || c is Bloom)
                {
                    c.active = false;
                    killed++;
                }
                else if (c is VisualEnvironment)
                {
                    var ve = (VisualEnvironment)c;
                    ve.skyType.value = 0;   // 0 = «нет неба»
                    ve.cloudType.value = 0; // 0 = «нет облаков»
                    killed++;
                }
            }
            v.profile = _skyKillClone;
            log = $"sky killed (comps={_skyKillClone.components.Count}, killed={killed})";
        }

        static void RestoreSky()
        {
            if (_skyVol != null && _savedSkyProfile != null) _skyVol.profile = _savedSkyProfile;
            if (_skyKillClone != null) Object.DestroyImmediate(_skyKillClone);
            _skyVol = null; _savedSkyProfile = null; _skyKillClone = null;
        }

        static GameObject BuildPhotoCamera(int layer, float keyLux, float fillLux)
        {
            var go = new GameObject("LakeBillboardPhotoCamera");

            // локальный объём: фиксированная экспозиция и выключенные туман/блум —
            // фото не зависят от времени суток сцены; камера слушает ТОЛЬКО этот объём
            var volGo = new GameObject("PhotoVolume");
            volGo.transform.SetParent(go.transform, false);
            volGo.layer = layer;
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1000f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var exp = profile.Add<Exposure>();
            exp.mode.value = ExposureMode.Fixed;
            exp.mode.overrideState = true;
            exp.fixedExposure.value = 2.5f;
            exp.fixedExposure.overrideState = true;
            var fog = profile.Add<Fog>();
            fog.enabled.value = false;
            fog.enabled.overrideState = true;
            // Bloom не отключаем: маска объёмов камеры видит только этот локальный
            // объём, поэтому сценарные объёмы с блумом на фото не влияют.
            vol.profile = profile;

            // два направленных света: ключевой спереди (свет идёт в −Z, как камера) + мягкий контровой
            var key = new GameObject("PhotoKeyLight");
            key.transform.SetParent(go.transform, false);
            var kl = key.AddComponent<Light>();
            kl.type = LightType.Directional;
            kl.intensity = keyLux;         // HDRP: направленные — в люксах (lightUnit=Lux)
            kl.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(30f, 185f, 0f);
            key.AddComponent<HDAdditionalLightData>();

            var fill = new GameObject("PhotoFillLight");
            fill.transform.SetParent(go.transform, false);
            var fl = fill.AddComponent<Light>();
            fl.type = LightType.Directional;
            fl.intensity = fillLux;
            fill.transform.rotation = Quaternion.Euler(12f, 340f, 0f);
            fill.AddComponent<HDAdditionalLightData>();

            // камера последней; HDRP при AddComponent<Camera> сам создаёт
            // HDAdditionalCameraData — пишем настройки в него, а не в свой второй
            var cam = go.AddComponent<Camera>();
            var hd = go.GetComponent<HDAdditionalCameraData>();
            if (hd == null) hd = go.AddComponent<HDAdditionalCameraData>();
            hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            hd.backgroundColorHDR = new Color(1f, 0f, 1f); // пурпур для хромакея
            hd.volumeLayerMask = 1 << layer;               // только наш локальный объём
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(1f, 0f, 1f);
            cam.cullingMask = 1 << layer;                  // снимаем только наше растение
            cam.orthographic = true;
            return go;
        }

        static bool BakeOne(string prefabPath, string stem, RenderTexture rt, int layer,
            out PlantBillboardEntry entry, PlantBillboardDatabase db)
        {
            entry = null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return false;

            // дубликаты: в других папках может лежать тот же стем — пропускаем уже снятое по требованию.
            var old = db.Get(stem);
            var existingPath = $"{OutDir}/{stem}.png";
            if (File.Exists(existingPath) && old == null)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(existingPath);
                if (tex != null)
                    old = new PlantBillboardEntry { prefabName = stem, photo = tex };
            }
            // reuse ТОЛЬКО с размерами: записи без них (манекены из короткого
            // reuse-пути) переснимаются — свежий кадр перезаписывает PNG, а в
            // базу уходят настоящие размеры, а не нули
            if (old != null && old.photo != null && old.sizeY > 0.0001f)
            {
                entry = old;
                return true;
            }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (inst == null) return false;
            SetLayerRecursive(inst.transform, layer);

            // фотографируем максимальную деталь: рендереры с именем *_LOD0.
            // ВАЖНО: LODGroup у бейк-камеры выбирает СВОЙ уровень по дистанции,
            // а транзишены паковских групп бывают больше 1 (у деревьев trans
            // LOD0 = 1.2) — при нашем отступе камера выбирает LOD1 и фото
            // выходит пустым. Поэтому LODGroup отключаем целиком и управляем
            // рендерерами вручную; инстанс в конце всё равно уничтожается.
            var lodGroup = inst.GetComponentInChildren<LODGroup>(true);
            if (lodGroup != null) lodGroup.enabled = false;

            List<Renderer> photoOn = new List<Renderer>();
            var allRends = inst.GetComponentsInChildren<Renderer>(true);
            foreach (var r in allRends)
                if (r != null && !(r is ParticleSystemRenderer) && r.name.EndsWith("_LOD0"))
                    photoOn.Add(r);
            if (photoOn.Count == 0 && lodGroup != null)
            {
                var lods = lodGroup.GetLODs();
                if (lods.Length > 0)
                    foreach (var r in lods[0].renderers)
                        if (r != null && !(r is ParticleSystemRenderer)) photoOn.Add(r);
            }
            if (photoOn.Count == 0)
                foreach (var r in allRends)
                    if (r != null && !(r is ParticleSystemRenderer)) photoOn.Add(r);

            // выключаем всё, что не входит в фото-набор
            foreach (var r in allRends)
                if (r != null && !photoOn.Contains(r)) r.enabled = false;

            var bounds = ComputeLocalBounds(inst.transform);
            if (bounds.size == Vector3.zero)
            {
                Object.DestroyImmediate(inst);
                return false;
            }
            float maxSide = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            float orthoSize = maxSide * 0.5f * PhotoFramingGap.x;
            float rootY = inst.transform.position.y; // пивот префаба в мире (фото ставится в (0,0,0))

            float pitch = 12f;
            Vector3 center = bounds.center;
            Vector3 dir = new Vector3(0f, Mathf.Sin(pitch * Mathf.Deg2Rad), Mathf.Cos(pitch * Mathf.Deg2Rad));
            Vector3 camPos = center + dir * (orthoSize * 2.5f + 0.5f);

            // ОПЫТ: изменения экспозиции громкости И интенсивности света на ЖИВОЙ
            // камере HDRP между ручными cam.Render() не применяются (данные камеры
            // кешируются) — свежая камера применяет всё всегда. Поэтому каждая
            // коррекция = пересоздание камеры с новым светом; RT остаётся общим.
            var prevActive = RenderTexture.active;
            var read = new Texture2D(PhotoSize, PhotoSize, TextureFormat.RGBA32, false);
            GameObject cur = null;
            float keyLux = 3500f, fillLux = 1200f;
            try
            {
                for (int pass = 0; pass < 4; pass++)
                {
                    if (cur == null)
                    {
                        cur = BuildPhotoCamera(layer, keyLux, fillLux);
                        cur.transform.position = camPos;
                        cur.transform.LookAt(center, Vector3.up);
                        var c = cur.GetComponent<Camera>();
                        c.orthographicSize = orthoSize;
                        c.nearClipPlane = 0.01f;
                        c.farClipPlane = orthoSize * 8f + 2f;
                        c.targetTexture = rt;
                    }

                    cur.GetComponent<Camera>().Render();
                    RenderTexture.active = rt;
                    read.ReadPixels(new Rect(0, 0, PhotoSize, PhotoSize), 0, 0);
                    read.Apply();
                    RenderTexture.active = prevActive;

                    var probe = read.GetPixels();
                    float blown = FractionBlown(probe, PhotoSize);
                    if (blown < 0.25f || pass == 3) break;

                    // следующая коррекция — через новую камеру (см. комментарий выше)
                    Object.DestroyImmediate(cur);
                    cur = null;
                    keyLux *= 0.25f;
                    fillLux *= 0.25f;
                }
            }
            finally
            {
                if (cur != null) Object.DestroyImmediate(cur);
            }

            // хромакей по HSV: близость ОТТЕНКА к пурпуру (300°) + насыщенность.
            // RGB-дистанция не годится: при высокой EV фон темнеет и тёмная
            // листва по яркости подтягивается к фону → полупрозрачная каша.
            // Тон фона остаётся ~300° при любой яркости, а тёмно-зелёная листва
            // (hue 110–140°) и белые кончики (sat≈0) уверенно остаются непрозрачными.
            var pixels = read.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                Color.RGBToHSV(c, out float hue, out float sat, out float val);
                float d = hue - (300f / 360f);
                d = Mathf.Repeat(d + 0.5f, 1f) - 0.5f;           // в [-0.5, 0.5)
                float hueDist = Mathf.Abs(d) * 2f;               // 0..1 (0 = точно пурпур)
                float hueN = 1f - Mathf.SmoothStep(0f, 1f, (hueDist - 0.10f) / 0.08f);
                float satN = Mathf.SmoothStep(0f, 1f, (sat - 0.12f) / 0.18f);
                float a = Mathf.Clamp01(1f - hueN * Mathf.Clamp01(satN));
                // despill: на кромках листва смешивается с пурпурным фоном —
                // вычитаем пурпурную компоненту (min(r,b) − g), иначе на фото
                // остаются сиреневые пятна и розовая окантовка
                float spill = Mathf.Min(c.r, c.b) - c.g;
                if (spill > 0f)
                {
                    c.r = Mathf.Max(0f, c.r - spill);
                    c.b = Mathf.Max(0f, c.b - spill);
                }
                pixels[i] = new Color(c.r * a, c.g * a, c.b * a, a);
            }
            read.SetPixels(pixels);
            read.Apply();

            // центры КАДРА в локальном фрейме корня (учёт поворота/масштаба):
            // квад в менеджере и внутри префаба ставится через эти значения,
            // а не через низ габарита — иначе высокое растение плывёт вверх
            // на ширину прозрачных полей кадра (у дерева ~1 м).
            // ВАЖНО: вычислять ДО DestroyImmediate — обращение к transform
            // уничтоженного объекта бросает MissingReferenceException
            var localCenter = inst.transform.InverseTransformPoint(bounds.center);

            var png = read.EncodeToPNG();
            File.WriteAllBytes(existingPath, png);
            Object.DestroyImmediate(read);
            Object.DestroyImmediate(inst);
            AssetDatabase.ImportAsset(existingPath);

            entry = new PlantBillboardEntry
            {
                prefabName = stem,
                photo = AssetDatabase.LoadAssetAtPath<Texture2D>(existingPath),
                sizeX = orthoSize * 2f,
                sizeY = orthoSize * 2f,
                offsetBottom = bounds.min.y - rootY,
                offsetY = localCenter.y,
                offsetX = localCenter.x,
                offsetZ = localCenter.z,
            };
            return entry.photo != null;
        }

        /// <summary>Доля пересвеченных пикселей СРЕДИ пикселей растения (пурпурный фон по HSV не считается).</summary>
        static float FractionBlown(Color[] px, int size)
        {
            int opaque = 0, blown = 0;
            foreach (var c in px)
            {
                Color.RGBToHSV(c, out float hue, out float sat, out float val);
                float d = hue - (300f / 360f);
                d = Mathf.Repeat(d + 0.5f, 1f) - 0.5f;
                float hueDist = Mathf.Abs(d) * 2f;
                float hueN = 1f - Mathf.SmoothStep(0f, 1f, (hueDist - 0.10f) / 0.08f);
                float satN = Mathf.SmoothStep(0f, 1f, (sat - 0.12f) / 0.18f);
                if (1f - hueN * Mathf.Clamp01(satN) < 0.5f) continue; // фон
                opaque++;
                if (val > 0.94f) blown++;
            }
            return opaque >= 30 ? (float)blown / opaque : 0f;
        }

        static Bounds ComputeLocalBounds(Transform t)
        {
            bool have = false;
            var b = new Bounds();
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                if (!have) { b = r.bounds; have = true; }
                else b.Encapsulate(r.bounds);
            }
            return b;
        }

        static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayerRecursive(t.GetChild(i), layer);
        }

        static void CreateDatabase(string path)
        {
            var db = ScriptableObject.CreateInstance<PlantBillboardDatabase>();
            AssetDatabase.CreateAsset(db, path);
        }

        static GameObject FindPrefab(string stem)
        {
            var guid = AssetDatabase.FindAssets($"t:Prefab {stem}", PlantFolders);
            foreach (var g in guid)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(path) == stem)
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }

        static int EnsureLayer(string name)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return -1;
            var so = new SerializedObject(assets[0]);
            var layers = so.FindProperty("layers");
            if (layers == null) layers = so.FindProperty("m_Layers"); // старые версии Unity
            if (layers == null) return -1;
            for (int i = 8; i < layers.arraySize; i++)
            {
                var sp = layers.GetArrayElementAtIndex(i);
                if (sp.stringValue == name) return i;
            }
            for (int i = 8; i < layers.arraySize; i++)
            {
                var sp = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = name;
                    so.ApplyModifiedProperties();
                    return i;
                }
            }
            return -1;
        }
    }
}
