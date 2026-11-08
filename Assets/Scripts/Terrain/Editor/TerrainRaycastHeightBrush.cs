using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Lake.TerrainTools
{
    /// <summary>
    /// Кисть «Set Height по лучу»: высота кисти берётся не со слайдера, а из точки,
    /// куда попадает луч из камеры сцены, пущенный через курсор.
    ///
    /// Работа:
    ///  * наводим курсор на землю в Scene view — рисуются круг кисти и маркер
    ///    целевой высоты (горизонтальный круг на той высоте, куда луч попал);
    ///  * ЛКМ (нажатие или удержание) — высота внутри круга тянется к высоте,
    ///    на которую луч попал в момент нажатия; цель держится до отпускания
    ///    кнопки, при этом кисть продолжает ходить за курсором — можно вести
    ///    плоскость ландшафта на фиксированной высоте;
    ///  * Ctrl+Z отменяет весь штрих целиком.
    ///
    /// Луч проверяется по Terrain-коллайдерам активных тайлов (берётся ближайший);
    /// если коллайдеры выключены — точка пересечения ищется маршем по сетке высот.
    /// Кисть красит все тайлы, которых касается круг, — швы между тайлами не остаются.
    /// Работает только в режиме редактирования (в плее встроенные методы Unity
    /// изменяли бы сохраняемый ассет TerrainData — опасно).
    ///
    /// Меню: Tools/Lake/Terrain/Set Height by Raycast.
    /// </summary>
    public sealed class TerrainRaycastHeightBrush : EditorWindow
    {
        const float MaxRayDistance = 8000f;   // предел Physics-луча
        const float MaxMarchDistance = 4000f; // пределрезервного марша по высотам

        [MenuItem("Tools/Lake/Terrain/Set Height by Raycast")]
        static void Open()
        {
            GetWindow<TerrainRaycastHeightBrush>("Set Height × луч").minSize = new Vector2(300, 240);
        }

        bool _active = true;
        float _radius = 8f;
        float _strength = 1f;

        // --- состояние штриха ---
        bool _painting;
        int _undoGroup;
        readonly HashSet<Terrain> _stroked = new HashSet<Terrain>();
        float _targetWorldY;   // высота, к которой тянем ландшафт (мировая)
        bool _hasTarget;
        Vector3 _hitPoint;     // последняя точка под курсором (для маркера)
        bool _hasHit;
        Terrain _hitTerrain;

        bool _subscribed;

        void OnEnable()
        {
            Subscribe(true);
        }

        void OnDisable()
        {
            Subscribe(false);
            EndStroke(true);
        }

        void Subscribe(bool on)
        {
            if (_subscribed == on) return;
            _subscribed = on;
            if (on) SceneView.duringSceneGui += OnSceneGui;
            else SceneView.duringSceneGui -= OnSceneGui;
            SceneView.RepaintAll();
        }

        // ------------------------------------------------------------- GUI окна

        void OnGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("Инструмент работает только в режиме редактирования.\nВыйдите из плея.", MessageType.Warning);
                if (_painting) EndStroke(true);
                return;
            }

            _active = EditorGUILayout.Toggle(
                new GUIContent("Кисть активна", "Пока включено — ЛКМ в Scene view красит высоту, а не выбирает объекты"),
                _active);
            if (!_active)
                EditorGUILayout.HelpBox("Кисть выключена — клики в Scene view работают как обычно.", MessageType.Info);

            _radius = EditorGUILayout.Slider(new GUIContent("Радиус, м"), _radius, 0.5f, 150f);
            _strength = EditorGUILayout.Slider(new GUIContent("Сила"), _strength, 0.05f, 1f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Целевая высота", _hasTarget ? $"{_targetWorldY:0.00} м (мир)" : "—");
            EditorGUILayout.LabelField("Точка луча", _hasHit
                ? $"{_hitPoint.x:0.0}  {_hitPoint.y:0.00}  {_hitPoint.z:0.0}"
                : "—");
            EditorGUILayout.LabelField("Тайл", _hitTerrain != null ? _hitTerrain.name : "—");

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "ЛКМ в Scene view: высота цели фиксируется по лучу в точке нажатия\n" +
                "и держится до отпускания кнопки (кисть ходит за курсором).\n" +
                "Alt+ЛКМ — обычный облёт камеры. Ctrl+Z отменяет штрих целиком.", MessageType.None);
        }

        // ----------------------------------------------------- события Scene view

        void OnSceneGui(SceneView view)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (_painting) EndStroke(true);
                return;
            }
            if (!_active) return;

            Event e = Event.current;
            if (e == null) return;

            // маркер кисти обновляем на перерисовке, если не идёт штрих
            if (e.type == EventType.Repaint && !_painting)
                UpdateHit(view);

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && !e.alt && CaptureTarget(view))
                    {
                        BeginStroke();
                        ApplyBrush();
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    // цель зафиксирована в момент нажатия и держится до отпускания;
                    // кисть при этом продолжает ходить за курсором
                    if (_painting && e.button == 0 && !e.alt && UpdateHit(view))
                    {
                        ApplyBrush();
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (_painting && e.button == 0)
                    {
                        EndStroke(true);
                        e.Use();
                    }
                    break;

                case EventType.MouseLeaveWindow:
                    if (_painting) EndStroke(true);
                    _hasHit = false;
                    break;
            }

            DrawBrushHandles();
        }

        // ------------------------------------------------------------- луч и цель

        /// <summary>Обновляет точку попадания луча под курсором (без смены цели). false — луч в небо.</summary>
        bool UpdateHit(SceneView view)
        {
            if (view == null || view.camera == null) return false;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (!RaycastTerrains(ray, out var point, out var terrain, out _))
            {
                _hasHit = false;
                return false;
            }
            _hitPoint = point;
            _hitTerrain = terrain;
            _hasHit = true;
            return true;
        }

        /// <summary>Найти точку попадания луча и взять её высоту как цель. false — луч в небо.</summary>
        bool CaptureTarget(SceneView view)
        {
            if (!UpdateHit(view)) return false;
            _targetWorldY = _hitPoint.y;
            _hasTarget = true;
            return true;
        }

        /// <summary>
        /// Ближайшее пересечение луча с Terrain-коллайдерами активных тайлов;
        /// если коллайдеров нет — марш по сетке высот.
        /// </summary>
        static bool RaycastTerrains(Ray ray, out Vector3 point, out Terrain terrain, out float distance)
        {
            point = default;
            terrain = null;
            distance = float.PositiveInfinity;

            var hits = Physics.RaycastAll(ray, MaxRayDistance, -1, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (h.distance <= 0.05f) continue;
                if (!(h.collider is TerrainCollider)) continue;
                var t = h.collider.GetComponent<Terrain>();
                if (t == null || !t.isActiveAndEnabled || t.terrainData == null) continue;
                if (h.distance < distance)
                {
                    distance = h.distance;
                    point = h.point;
                    terrain = t;
                }
            }
            if (terrain != null) return true;

            return MarchHeightfield(ray, out point, out terrain, out distance);
        }

        /// <summary>Резервный поиск пересечения луча с рельефом, когда Terrain-коллайдеры выключены.</summary>
        static bool MarchHeightfield(Ray ray, out Vector3 point, out Terrain terrain, out float distance)
        {
            point = default;
            terrain = null;
            distance = float.PositiveInfinity;

            float dist = 0.5f;
            float prevAbove = SampleDelta(ray, dist);

            while (dist < MaxMarchDistance)
            {
                float next = dist + Mathf.Max(1f, dist * 0.02f);
                float above = SampleDelta(ray, next);

                if (prevAbove > 0f && above <= 0f)
                {
                    float lo = dist, hi = next;
                    for (int i = 0; i < 12; i++)
                    {
                        float mid = (lo + hi) * 0.5f;
                        if (SampleDelta(ray, mid) <= 0f) hi = mid;
                        else lo = mid;
                    }
                    distance = (lo + hi) * 0.5f;
                    point = ray.GetPoint(distance);
                    terrain = TerrainAt(point);
                    return terrain != null;
                }

                prevAbove = above;
                dist = next;
            }
            return false;
        }

        /// <summary>Насколько точка луча выше рельефа (PositiveInfinity — вне тайлов).</summary>
        static float SampleDelta(Ray ray, float dist)
        {
            Vector3 p = ray.GetPoint(dist);
            float best = float.PositiveInfinity;
            foreach (var t in Terrain.activeTerrains)
            {
                if (t == null || t.terrainData == null) continue;
                Vector3 o = t.transform.position;
                Vector3 size = t.terrainData.size;
                if (p.x < o.x || p.x > o.x + size.x || p.z < o.z || p.z > o.z + size.z) continue;
                float hWorld = o.y + t.SampleHeight(p);
                best = Mathf.Min(best, p.y - hWorld);
            }
            return best;
        }

        static Terrain TerrainAt(Vector3 world)
        {
            foreach (var t in Terrain.activeTerrains)
            {
                if (t == null || t.terrainData == null) continue;
                Vector3 o = t.transform.position;
                Vector3 size = t.terrainData.size;
                if (world.x >= o.x && world.x <= o.x + size.x &&
                    world.z >= o.z && world.z <= o.z + size.z)
                    return t;
            }
            return null;
        }

        // ---------------------------------------------------------------- кисть

        void BeginStroke()
        {
            _painting = true;
            _stroked.Clear();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Terrain Set Height (Ray Brush)");
            _undoGroup = Undo.GetCurrentGroup();
        }

        void EndStroke(bool collapse)
        {
            if (!_painting) return;
            _painting = false;
            _stroked.Clear();
            if (collapse) Undo.CollapseUndoOperations(_undoGroup);
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Тянет высоты всех тайлов, которых касается круг кисти, к целевой высоте.
        /// В центре круга высота совпадает с целью, к кромке плавно спадает к исходной.
        /// </summary>
        void ApplyBrush()
        {
            if (!_active || !_hasTarget || _radius < 0.05f) return;

            Vector3 c = _hitPoint;
            float r = _radius;

            foreach (var terrain in Terrain.activeTerrains)
            {
                if (terrain == null || terrain.terrainData == null) continue;
                var td = terrain.terrainData;
                Vector3 o = terrain.transform.position;
                Vector3 size = td.size;

                // круг касается этого тайла?
                float nx = Mathf.Clamp(c.x - o.x, 0f, size.x);
                float nz = Mathf.Clamp(c.z - o.z, 0f, size.z);
                float dx = c.x - (o.x + nx);
                float dz = c.z - (o.z + nz);
                if (dx * dx + dz * dz > r * r) continue;

                int hw = td.heightmapResolution;
                float cellX = size.x / (hw - 1);
                float cellZ = size.z / (hw - 1);

                int cxPix = Mathf.RoundToInt((c.x - o.x) / cellX);
                int czPix = Mathf.RoundToInt((c.z - o.z) / cellZ);
                int rX = Mathf.CeilToInt(r / cellX);
                int rZ = Mathf.CeilToInt(r / cellZ);

                int x0 = Mathf.Clamp(cxPix - rX, 0, hw - 1);
                int z0 = Mathf.Clamp(czPix - rZ, 0, hw - 1);
                int x1 = Mathf.Clamp(cxPix + rX, 0, hw - 1);
                int z1 = Mathf.Clamp(czPix + rZ, 0, hw - 1);
                int w = x1 - x0 + 1;
                int h = z1 - z0 + 1;
                if (w <= 0 || h <= 0) continue;

                // undo-снимок делаем один раз на тайл за штрих
                if (_stroked.Add(terrain))
                    Undo.RecordObject(td, "Terrain Set Height (Ray Brush)");

                var heights = td.GetHeights(x0, z0, w, h);
                float targetN = Mathf.Clamp01((_targetWorldY - o.y) / size.y);

                for (int z = 0; z < h; z++)
                {
                    float pz = o.z + (z0 + z) * cellZ;
                    float dzPix = pz - c.z;
                    if (dzPix > r || dzPix < -r) continue;

                    for (int x = 0; x < w; x++)
                    {
                        float px = o.x + (x0 + x) * cellX;
                        float ddx = px - c.x;
                        float dist = Mathf.Sqrt(ddx * ddx + dzPix * dzPix);
                        if (dist >= r) continue;

                        // мягкий спад: 1 в центре → 0 на кромке
                        float weight = Mathf.SmoothStep(0f, 1f, 1f - dist / r);
                        if (weight <= 0f) continue;

                        heights[z, x] = Mathf.Lerp(heights[z, x], targetN, weight * _strength);
                    }
                }

                td.SetHeights(x0, z0, heights);
            }

            HandleUtility.Repaint();
        }

        // ---------------------------------------------------------------- маркер

        void DrawBrushHandles()
        {
            if (!_active || !_hasHit) return;

            float r = _radius;
            Handles.color = _painting ? new Color(0.35f, 1f, 0.5f, 0.9f) : new Color(1f, 0.9f, 0.2f, 0.9f);
            Handles.DrawWireDisc(_hitPoint, Vector3.up, r);
            Handles.DrawWireDisc(_hitPoint, Vector3.up, r * 0.35f);

            if (_hasTarget)
            {
                var t = new Vector3(_hitPoint.x, _targetWorldY, _hitPoint.z);
                Handles.color = new Color(1f, 1f, 1f, 0.8f);
                Handles.DrawDottedLine(_hitPoint, t, 2f);
                Handles.DrawWireDisc(t, Vector3.up, r * 0.55f);
                Handles.Label(t + Vector3.up * 0.5f, $" {_targetWorldY:0.00} м");
            }

            Handles.color = Color.white;
        }
    }
}
