using UnityEngine;

namespace Lake.Outline
{
    /// <summary>
    /// Управляет обводкой. Одновременно подсвечен только один объект.
    /// </summary>
    public class OutlineManager : MonoBehaviour
    {
        OutlineRenderer _current;

        public void SetTarget(Component component)
        {
            Highlightable target = null;
            if (component != null)
            {
                target = component.GetComponentInParent<Highlightable>();
                // Если IInteractable — на этом компоненте, а Highlightable — на родителе
                if (target == null) target = component.GetComponent<Highlightable>();
            }

            if (_current != null && (_current.Target != target))
            {
                _current.Dispose();
                _current = null;
            }

            if (target != null && _current == null)
            {
                _current = new OutlineRenderer(target);
                _current.Create();
            }
        }

        void OnDestroy() { _current?.Dispose(); }
    }
}