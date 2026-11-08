using System;
using System.Linq;
using UnityEngine;
using Lake.Core;

namespace Lake.Player
{
    /// <summary>
    /// Raycast из AimPivot (не из камеры!) — то, куда игрок смотрит оружием.
    /// Подписка на InputProvider в OnEnable — фикс бага "E не работает".
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [SerializeField] Transform rayOrigin;
        [SerializeField] float interactRange = 3f;
        [SerializeField] LayerMask interactMask = ~0;

        [Header("Debug")]
        [SerializeField] bool showDebugRay = true;
        [SerializeField] Color rayHitColor = Color.green;
        [SerializeField] Color rayMissColor = Color.red;
        [SerializeField] bool logTargetChanges = false;

        IInteractable _current;
        RaycastHit _lastHit;
        bool _hadHit;
        InputProvider _input;

        public event Action<IInteractable, string> OnTargetChanged;

        void OnEnable()
        {
            TrySubscribe();
        }

        void OnDisable()
        {
            if (_input != null)
            {
                _input.InteractPressed -= OnInteract;
                _input = null;
            }
        }

        void TrySubscribe()
        {
            if (_input != null) return;
            var provider = InputProvider.Instance;
            if (provider == null) return;
            _input = provider;
            _input.InteractPressed += OnInteract;
        }

        void Update()
        {
            TrySubscribe();
            if (rayOrigin == null) return;

            _hadHit = Physics.Raycast(rayOrigin.position, rayOrigin.forward, out _lastHit,
                                      interactRange, interactMask, QueryTriggerInteraction.Collide);

            IInteractable newTarget = null;
            if (_hadHit)
            {
                var hitObj = _lastHit.collider.gameObject;
                newTarget = hitObj.GetComponentInParent<IInteractable>();

                // DEBUG: логируем все компоненты на объекте и родителях
                if (logTargetChanges)
                {
                    var allComponents = hitObj.GetComponents<Component>();
                    var compNames = string.Join(", ", allComponents.Select(c => c.GetType().Name));
                    Debug.Log($"[Interactor] Raycast hit: {hitObj.name} | Components: [{compNames}] | Distance: {_lastHit.distance:F2}m");
                }

                if (newTarget == null && logTargetChanges)
                {
                    // Также проверяем родителей
                    var parent = hitObj.transform.parent;
                    while (parent != null)
                    {
                        var parentComps = parent.GetComponents<Component>();
                        var parentCompNames = string.Join(", ", parentComps.Select(c => c.GetType().Name));
                        Debug.Log($"[Interactor]   Parent '{parent.name}' Components: [{parentCompNames}]");
                        parent = parent.parent;
                    }
                }
            }

            if (newTarget != _current)
            {
                _current = newTarget;
                if (logTargetChanges)
                {
                    if (_current != null)
                        Debug.Log($"[Interactor] Target acquired: {_current.GetType().Name} on {_lastHit.collider.name} — Prompt: \"{_current.Prompt}\"");
                    else
                        Debug.Log("[Interactor] Target lost");
                }
                OnTargetChanged?.Invoke(_current, _current?.Prompt);
            }
        }

        void OnInteract()
        {
            if (_current != null && _current.CanInteract(gameObject))
            {
                if (logTargetChanges)
                    Debug.Log($"[Interactor] Interacting with: {_current.GetType().Name} on {_lastHit.collider.name}");
                _current.Interact(gameObject);
            }
            else if (logTargetChanges)
            {
                if (_hadHit)
                {
                    var hitCol = _lastHit.collider;
                    var hitObj = hitCol.gameObject;
                    var parents = GetHierarchyPath(hitObj.transform);
                    Debug.Log($"[Interactor] Interact pressed but NO valid IInteractable on hit object:" +
                              $"\n  Hit: {hitObj.name} (layer: {LayerMask.LayerToName(hitObj.layer)}, tag: {hitObj.tag})" +
                              $"\n  Collider: {hitCol.GetType().Name}" +
                              $"\n  Hierarchy: {parents}" +
                              $"\n  Distance: {_lastHit.distance:F2}m");
                }
                else
                {
                    Debug.Log("[Interactor] Interact pressed but nothing hit (raycast miss)");
                }
            }
        }

        static string GetHierarchyPath(Transform t)
        {
            var path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + " > " + path;
            }
            return path;
        }

        void OnDrawGizmosSelected()
        {
            if (!showDebugRay || rayOrigin == null) return;

            Gizmos.color = _hadHit ? rayHitColor : rayMissColor;
            Vector3 origin = rayOrigin.position;
            Vector3 dir = rayOrigin.forward;
            Vector3 end = origin + dir * interactRange;

            // Основной луч
            Gizmos.DrawRay(origin, dir * interactRange);

            // Точка попадания
            if (_hadHit)
            {
                Gizmos.DrawWireSphere(_lastHit.point, 0.05f);
                Gizmos.DrawLine(_lastHit.point, _lastHit.point + _lastHit.normal * 0.1f);
            }

            // Сфера в конце луча (дистанция взаимодействия)
            Gizmos.color = new Color(rayMissColor.r, rayMissColor.g, rayMissColor.b, 0.2f);
            Gizmos.DrawWireSphere(end, 0.1f);
        }

        void OnDrawGizmos()
        {
            // Всегда рисуем тонкий луч для ориентации, даже когда не выбран
            if (!showDebugRay || rayOrigin == null) return;

            Gizmos.color = new Color(rayMissColor.r, rayMissColor.g, rayMissColor.b, 0.3f);
            Gizmos.DrawRay(rayOrigin.position, rayOrigin.forward * interactRange);
        }
    }
}