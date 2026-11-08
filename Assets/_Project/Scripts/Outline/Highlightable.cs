using UnityEngine;

namespace Lake.Outline
{
    /// <summary>
    /// Маркер: этот объект может быть подсвечен OutlineManager.
    /// Добавляется на корень pickup'а, мишени или любого интерактивного объекта.
    /// </summary>
    public class Highlightable : MonoBehaviour
    {
        public Color HighlightColor = new(1f, 0.85f, 0.2f, 1f);
        public float ScaleMultiplier = 1.06f;
    }
}