using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lake.Vegetation
{
    /// <summary>
    /// Автоматический билбординг растений: менеджер сам находит растения в сцене
    /// (по совпадению имени корня с записями PlantBillboardDatabase), читает их
    /// LODGroup и, когда камера удаляется дальше порога последнего LOD, прячет
    /// меш-LOD и подставляет квад с фотографией растения.
    ///
    /// Правила:
    ///  * скан сцены — один раз при старте, рекурсивно от корней;
    ///    внутри найденного растения потомков не трогает (они часть растения);
    ///  * растения без LODGroup билбордятся только в отладке (forceAll) —
    ///    у них нет понятия «последний LOD»;
    ///  * скрытие мешей — через snapshot включённости рендереров: при возврате
    ///    туда, где был виден последний LOD, всё восстанавливается;
    ///  * квад имеет свой «нижний пивот»: он стоит на земле даже при пивоте
    ///    префаба не на уровне земли;
    ///  * растёт в batch — проверки дистанции размазаны по кадрам;
    ///  * коллайдеры растений не трогаются — память о коллизиях не пропадает.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class PlantBillboardManager : MonoBehaviour
    {
        public PlantBillboardDatabase database;

        [Tooltip("Во сколько раз дальше последнего LOD включается билборд (лёгкий запас)")]
        public float engageFactor = 1.35f;

        [Tooltip("Сколько растений проверять на дистанцию за один кадр")]
        public int checkBatch = 250;

        public bool DebugForceAll { get; set; }

        // ----------------------------------------------------------- runtime

        static PlantBillboardManager _s;
        public static PlantBillboardManager Instance => _s;

        class Item
        {
            public Transform root;
            public PlantBillboardEntry entry;
            public LODGroup lod;
            public Renderer[] renderers;
            public bool[] states;      // snapshot включённости рендереров
            public GameObject quad;
            public bool showing;
            public float lastFrac;     // порог последнего LOD (0..1)
            public float worldSize;    // габарит меша (макс. измерение)
            public float engageSq;     // дистанция включённости² (с гистерезисом через exit-фактор)
            public bool engageReady;
        }

        readonly List<Item> _items = new List<Item>();
        readonly HashSet<GameObject> _scanned = new HashSet<GameObject>();
        Camera _camera;
        int _scanIndex;
        Transform _quadContainer;
        int _shownCount;

        public int Scanned => _items.Count;
        public int Shown => _shownCount;
        public int Entries => database != null && database.entries != null ? database.entries.Count : 0;

        void Awake()
        {
            _s = this;
        }

        void OnEnable()
        {
            _s = this;
        }

        void OnDisable()
        {
            if (_s == this) _s = null;
        }

        void Start()
        {
            if (database == null)
            {
                enabled = false;
                Debug.LogWarning("[PlantBillboard] database не назначен — билбординг отключён");
                return;
            }
            database.RebuildMap();

            _quadContainer = transform;
            ScanScene();
            Debug.Log($"[PlantBillboard] сцена отсканирована: растений {_items.Count} из {Entries} типов");
        }

        void Update()
        {
            if (_items.Count == 0) return;

            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return;
            }

            int batch = Mathf.Max(1, checkBatch);
            int end = Mathf.Min(_items.Count, _scanIndex + batch);
            for (int i = _scanIndex; i < end; i++) CheckItem(_items[i], _camera);
            if (end >= _items.Count) _scanIndex = 0; else _scanIndex = end;
        }

        void LateUpdate()
        {
            // ориентация активных квадов на камеру (только yaw)
            if (Camera.main == null || _shownCount == 0) return;
            var cam = Camera.main;
            Vector3 cp = cam.transform.position;
            foreach (var it in _items)
            {
                if (!it.showing || it.quad == null) continue;
                Vector3 d = cp - it.quad.transform.position;
                d.y = 0f;
                if (d.sqrMagnitude < 1e-6f) continue;
                it.quad.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
            }
        }

        // ------------------------------------------------------------ скан

        void ScanScene()
        {
            var scene = gameObject.scene;
            _scanned.Clear();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == gameObject) continue;
                Scan(root.transform);
            }
        }

        void Scan(Transform t)
        {
            if (_scanned.Contains(t.gameObject)) return;
            _scanned.Add(t.gameObject);

            var entry = database.Get(EntryName(t.gameObject.name));
            if (entry != null)
            {
                // у префабов со вшитым билборд-LOD (квады BB_* последним уровнем
                // LODGroup) переключением управляет сама Unity — менеджер не нужен
                if (!HasEmbeddedBillboardLOD(t)) AddItem(t.gameObject, entry);
                return; // потомки — часть растения, дальше не идём
            }
            for (int i = 0; i < t.childCount; i++)
                Scan(t.GetChild(i));
        }

        /// <summary>Есть ли уровень LODGroup из квадов BB_* — признак вшитого билборда.</summary>
        static bool HasEmbeddedBillboardLOD(Transform root)
        {
            var lg = root.GetComponentInChildren<LODGroup>(true);
            if (lg == null) return false;
            foreach (var l in lg.GetLODs())
                foreach (var r in l.renderers)
                    if (r != null && r.name.StartsWith("BB_", StringComparison.Ordinal)) return true;
            return false;
        }

        static string EntryName(string name)
        {
            name = name.Replace("(Clone)", "").Trim();
            var space = name.IndexOf(" (", StringComparison.Ordinal);
            if (space > 0) name = name.Substring(0, space);
            return name;
        }

        void AddItem(GameObject go, PlantBillboardEntry entry)
        {
            var item = new Item
            {
                root = go.transform,
                entry = entry,
                lod = go.GetComponentInChildren<LODGroup>(true),
            };

            var rendList = new List<Renderer>(8);
            AddRend(go.transform, rendList);
            item.renderers = rendList.ToArray();
            item.states = new bool[item.renderers.Length];
            for (int i = 0; i < item.renderers.Length; i++)
                item.states[i] = item.renderers[i].enabled;

            // габарит меша по мировым боксам рендереров
            bool have = false;
            var b = new Bounds();
            foreach (var r in item.renderers)
            {
                if (r == null) continue;
                if (!have) { b = r.bounds; have = true; }
                else b.Encapsulate(r.bounds);
            }
            item.worldSize = have ? Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) : 1f;

            // порог последнего LOD группы
            float lastFrac = 0f;
            if (item.lod != null && item.lod.enabled)
            {
                var lods = item.lod.GetLODs();
                if (lods.Length > 0)
                {
                    lastFrac = lods[lods.Length - 1].screenRelativeTransitionHeight;
                    if (lastFrac >= 1f) lastFrac = 0f;
                }
            }
            item.lastFrac = lastFrac;

            _items.Add(item);
        }

        static void AddRend(Transform t, List<Renderer> list)
        {
            var r = t.GetComponent<Renderer>();
            if (r != null) list.Add(r);
            for (int i = 0; i < t.childCount; i++)
                AddRend(t.GetChild(i), list);
        }

        // ------------------------------------------------------------ своп

        void CheckItem(Item it, Camera cam)
        {
            if (it.root == null)
            {
                if (it.quad != null) Destroy(it.quad);
                if (it.showing) _shownCount--;
                it.showing = false;
                return;
            }

            if (!it.engageReady)
            {
                if (it.lastFrac <= 1e-4f) { it.engageReady = true; it.engageSq = -1; return; }
                float tanHalf = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float d = it.worldSize / (2f * tanHalf * it.lastFrac) * Mathf.Max(1.01f, engageFactor);
                d = Mathf.Clamp(d, 12f, 600f);
                it.engageSq = d * d;
                it.engageReady = true;
            }

            bool want;
            if (DebugForceAll) want = it.engageSq > 0 || it.lastFrac > 0f; // forceAll: только для растений с LOD
            else if (it.engageSq <= 0f) want = false;
            else
            {
                float ds = (cam.transform.position - it.root.position).sqrMagnitude;
                want = it.showing ? ds > it.engageSq * 0.81f : ds > it.engageSq; // гистерезис 10%
            }

            if (want && !it.showing) Show(it);
            else if (!want && it.showing) Hide(it);
        }

        void Show(Item it)
        {
            it.showing = true;
            _shownCount++;

            // quad
            if (it.quad == null)
            {
                it.quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                var col = it.quad.GetComponent<Collider>();
                if (col != null) Destroy(col);
                it.quad.name = "BB_" + it.entry.prefabName;

                var mr = it.quad.GetComponent<MeshRenderer>();
                mr.sharedMaterial = EnsureMaterial(it.entry);
                it.quad.transform.SetParent(_quadContainer, false);
                it.quad.transform.hideFlags = HideFlags.DontSave;

                var rs = it.root.lossyScale;
                it.quad.transform.localScale = new Vector3(
                    it.entry.sizeX * Mathf.Abs(rs.x),
                    it.entry.sizeY * Mathf.Abs(rs.y), 1f);
                // квад центрируется по КОНТЕНТУ фото: offsets — центр кадра в
                // локальном фрейме корня (снятый при беке через
                // InverseTransformPoint), а не низ габарита растения
                it.quad.transform.position = it.root.TransformPoint(
                    new Vector3(it.entry.offsetX, it.entry.offsetY, it.entry.offsetZ));
            }
            it.quad.SetActive(true);

            // прятать реальные меши
            for (int i = 0; i < it.renderers.Length; i++)
                if (it.renderers[i] != null) it.renderers[i].enabled = false;
            if (it.lod != null) it.lod.enabled = false;
        }

        void Hide(Item it)
        {
            it.showing = false;
            _shownCount--;
            if (it.quad != null) it.quad.SetActive(false);

            for (int i = 0; i < it.renderers.Length; i++)
                if (it.renderers[i] != null) it.renderers[i].enabled = it.states[i];
            if (it.lod != null) it.lod.enabled = true;
        }

        // ---------------------------------------------------------- материалы

        static Material EnsureMaterial(PlantBillboardEntry e)
        {
            if (e.material == null)
            {
                // HDRP/Lit, а НЕ Unlit: unlit игнорирует свет сцены — квад
                // светится фотографией в сумерках/ночью. Lit затеняет квад
                // вместе со сценой. Настройки зеркалят ApplyBillboardMaterial
                // в редакторном билдере (у них общая палитра свойств).
                var m = new Material(Shader.Find("HDRP/Lit"));
                m.SetTexture("_BaseColorMap", e.photo);
                m.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.85f, 1f));

                // alpha-клип: прозрачность по фотографии, поверхность непрозрачная.
                // ВАЖНО: у HDRP-шейдеров клип управляется КЛЮЧЕВЫМ СЛОВОМ
                // #pragma shader_feature_local _ALPHATEST_ON — установки
                // _AlphaCutoffEnable без кея игнорируются (квад рисуется
                // чёрным прямоугольником — фон текстуры 0 альфа, не отсечён)
                m.SetFloat("_SurfaceType", 0f);              // Opaque
                m.SetFloat("_AlphaCutoffEnable", 1f);
                m.SetFloat("_AlphaCutoff", 0.45f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_ZWrite", 1f);
                m.SetFloat("_BlendMode", 0f);
                m.SetFloat("_EnableFogOnTransparent", 0f);

                // без бликов и металла
                m.SetFloat("_Metallic", 0f);
                m.SetFloat("_Smoothness", 0f);

                // двусторонний рендер: примитивный Quad виден только со стороны
                // локальной −Z; отключаем отсечение задних граней, чтобы квад
                // не исчезал при LookRotation в сторону игрока
                m.SetFloat("_CullMode", (float)UnityEngine.Rendering.CullMode.Off);
                m.SetFloat("_OpaqueCullMode", (float)UnityEngine.Rendering.CullMode.Off);

                // билборд не тень и не трекер скорости
                m.SetShaderPassEnabled("ShadowCaster", false);
                m.SetShaderPassEnabled("MotionVectors", false);

                m.renderQueue = 2000; // Geometry
                e.material = m;
            }
            return e.material;
        }

        // ---------------------------------------------------------- отладка

        /// <summary>Команда протобруса: немедленно обойти все айтемы и пересчитать свопинг.</summary>
        public void ForceRescan()
        {
            _items.Clear();
            _shownCount = 0;
            _scanned.Clear();
            _scanIndex = 0;
            ScanScene();
        }
    }
}
