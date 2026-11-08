using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lake.Vegetation
{
    /// <summary>Данные одного растения для билборд-системы.</summary>
    [Serializable]
    public class PlantBillboardEntry
    {
        /// <summary>Базовое имя корня префаба (без "(Clone)" и суффиксов « (1)»).</summary>
        public string prefabName;

        /// <summary>«Фотография» растения: PNG с альфой на прозрачном фоне.</summary>
        public Texture2D photo;

        /// <summary>Сторона квада по X в метрах (квады в пейке квадратные: sizeX == sizeY).</summary>
        public float sizeX;

        /// <summary>Сторона квада по Y в метрах.</summary>
        public float sizeY;

        /// <summary>Сдвиг низа квада до опоры: bounds.min.y − пивот корня, обычно 0 (совместимость).</summary>
        public float offsetBottom;

        /// <summary>Центр кадра по Y, в ЛОКАЛЬНОМ фрейме корня (InverseTransformPoint): угол/масштаб учтены.</summary>
        public float offsetY;

        /// <summary>Центр кадра по X/Z, локальный фрейм корня: куст может быть асимметричен.</summary>
        public float offsetX;

        /// <summary>Центр кадра по Z, локальный фрейм корня.</summary>
        public float offsetZ;

        /// <summary>Материал билборда этого растения (создаётся менеджером лениво, в ассет не пишется).</summary>
        [NonSerialized] public Material material;
    }

    /// <summary>
    /// База данных билбордов: фото всех растений пака + метаданные для растяжки квадов.
    /// Собирается автоматически инструментом Tools/Lake/Vegetation/Bake Plant Photos.
    /// </summary>
    [CreateAssetMenu(menuName = "Lake/Plant Billboard Database", fileName = "PlantBillboardDatabase")]
    public class PlantBillboardDatabase : ScriptableObject
    {
        public List<PlantBillboardEntry> entries = new List<PlantBillboardEntry>();

        /// <summary>
        /// Ищет запись по имени корня сцены: точное совпадение или префикс вида
        /// «имя (N)» (Unity добавляет счётчик к дубликатам).
        /// </summary>
        public PlantBillboardEntry Get(string rootName)
        {
            if (string.IsNullOrEmpty(rootName) || entries == null) return null;

            if (_map.TryGetValue(rootName, out var exact)) return exact;

            foreach (var e in entries)
            {
                var stem = e.prefabName;
                if (string.IsNullOrEmpty(stem)) continue;
                if (rootName.Length > stem.Length &&
                    rootName.StartsWith(stem, StringComparison.OrdinalIgnoreCase) &&
                    (rootName[stem.Length] == ' ' || rootName[stem.Length] == '('))
                    return e;
            }
            return null;
        }

        /// <summary>Пересобрать быстрый индекс по точным именам.</summary>
        public void RebuildMap()
        {
            _map.Clear();
            if (entries == null) return;
            foreach (var e in entries)
                if (!string.IsNullOrEmpty(e.prefabName) && e.photo != null)
                    _map[e.prefabName] = e;
        }

        readonly Dictionary<string, PlantBillboardEntry> _map = new Dictionary<string, PlantBillboardEntry>();
    }
}
