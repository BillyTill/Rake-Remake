using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lake.Bootstrap
{
    /// <summary>
    /// Метка «здесь появляется игрок». Ставится на сцену как обычный объект:
    /// позиция метки и есть точка спавна. Если метки в сцене нет, спавн
    /// берётся из LakeSettings — так старые сцены продолжают работать.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Lake/Player Spawn Point")]
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        [Tooltip("Опустить игрока на поверхность земли под меткой. Выключите, " +
                 "если метка уже стоит там, где нужно, и просадка мешает.")]
        [SerializeField] private bool snapToGround = true;

        [Tooltip("Зазор над землёй в метрах: капсула игрока должна встать на грунт, " +
                 "а не внутрь него.")]
        [SerializeField] private float groundOffset = 0.05f;

        public bool SnapToGround => snapToGround;

        /// <summary>Куда именно встанет игрок — с учётом просадки на землю.</summary>
        public Vector3 ResolvePosition()
        {
            if (!snapToGround)
                return transform.position;

            if (!GroundProbe.TryGetGroundHeight(transform.position, out float ground))
                return transform.position;

            return new Vector3(transform.position.x, ground + groundOffset, transform.position.z);
        }

        /// <summary>
        /// Куда игрок будет смотреть. Наклон метки игнорируем: контроллер всё
        /// равно ставит камеру по горизонтали, а кривой поворот лишь путает
        /// при расстановке.
        /// </summary>
        public Quaternion ResolveRotation()
        {
            return Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        /// <summary>
        /// Первая метка сцены. Если меток несколько — берём первую и
        /// предупреждаем: молчаливый выбор одной из них выглядит как
        /// «спавн не там, где я поставил».
        /// </summary>
        public static bool TryFind(UnityEngine.SceneManagement.Scene scene, out PlayerSpawnPoint point)
        {
            point = null;

            if (!scene.IsValid() || !scene.isLoaded)
                return false;

            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<PlayerSpawnPoint>(true);
                if (found == null)
                    continue;

                if (point == null)
                {
                    point = found;
                    continue;
                }

                Debug.LogWarning($"[Lake] Меток спавна в сцене несколько: '{point.name}' и '{found.name}'. " +
                                 "Использую первую — оставьте одну метку.");
                break;
            }

            return point != null;
        }

        private void OnDrawGizmos()
        {
            // Габариты капсулы игрока: рост 1.8, центр контроллера на 0.9.
            const float height = 1.8f;
            const float radius = 0.32f;

            var spawn = ResolvePosition();
            var rotation = ResolveRotation();

            Gizmos.color = new Color(0.35f, 0.9f, 1f, 0.9f);
            Gizmos.DrawWireSphere(spawn + Vector3.up * radius, radius);
            Gizmos.DrawWireSphere(spawn + Vector3.up * (height - radius), radius);
            Gizmos.DrawLine(spawn + Vector3.up * radius + Vector3.left * radius,
                            spawn + Vector3.up * (height - radius) + Vector3.left * radius);
            Gizmos.DrawLine(spawn + Vector3.up * radius + Vector3.right * radius,
                            spawn + Vector3.up * (height - radius) + Vector3.right * radius);

            // Взгляд: стрелка по рысканию метки.
            var forward = rotation * Vector3.forward;
            var tail = spawn + Vector3.up * (height * 0.6f);
            var tip = tail + forward * 1.2f;

            Gizmos.color = new Color(0.6f, 1f, 0.4f, 1f);
            Gizmos.DrawLine(tail, tip);
            Gizmos.DrawLine(tip, tip - forward * 0.3f + rotation * Vector3.left * 0.2f);
            Gizmos.DrawLine(tip, tip - forward * 0.3f + rotation * Vector3.right * 0.2f);

            // Позиция уехала от метки из-за просадки на землю — показываем связь.
            if ((spawn - transform.position).sqrMagnitude > 0.0001f)
            {
                Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.6f);
                Gizmos.DrawLine(transform.position, spawn);
            }
        }
    }
}
