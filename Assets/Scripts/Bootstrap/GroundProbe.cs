using UnityEngine;

namespace Lake.Bootstrap
{
    /// <summary>
    /// Поиск поверхности земли под точкой. Нужен и метке спавна, и проверке
    /// «не под землёй ли точка» — держим логику в одном месте, чтобы они
    /// не разошлись в том, что считать землёй.
    /// </summary>
    internal static class GroundProbe
    {
        /// <summary>
        /// Высота земли под точкой. Сначала опрашиваем Terrain напрямую: это
        /// работает и когда физическая версия рельефа огрублена, и когда
        /// Terrain Collider выключен. Если рельефа нет — ищем по коллайдеру.
        /// </summary>
        public static bool TryGetGroundHeight(Vector3 position, out float height)
        {
            // Тайлов в сцене несколько, а activeTerrain отдаёт произвольный.
            var terrains = Terrain.activeTerrains;
            if (terrains != null)
            {
                foreach (var terrain in terrains)
                {
                    if (terrain == null || terrain.terrainData == null)
                        continue;

                    Vector3 size = terrain.terrainData.size;
                    Vector3 local = position - terrain.transform.position;

                    // За границами тайла высот нет.
                    if (local.x < 0f || local.x > size.x || local.z < 0f || local.z > size.z)
                        continue;

                    // SampleHeight уже возвращает мировую высоту, прибавлять
                    // transform.position.y второй раз нельзя.
                    height = terrain.SampleHeight(position);
                    return true;
                }
            }

            return TryRaycastTerrain(position, out height);
        }

        /// <summary>
        /// Запасной путь для сцен без Terrain. Луч короткий и фильтруется по
        /// типу коллайдера: длинный цепляет кроны и стволы, и точка спавна
        /// уезжает на дерево.
        /// </summary>
        private static bool TryRaycastTerrain(Vector3 position, out float height)
        {
            var origin = position + Vector3.up * 5f;
            var hits = Physics.RaycastAll(origin, Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore);

            bool found = false;
            float top = float.NegativeInfinity;

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.GetComponent<TerrainCollider>() == null)
                    continue;

                if (hit.point.y > top)
                {
                    top = hit.point.y;
                    found = true;
                }
            }

            height = found ? top : 0f;
            return found;
        }
    }
}
