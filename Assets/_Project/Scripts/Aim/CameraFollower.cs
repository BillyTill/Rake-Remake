using UnityEngine;

namespace Lake.Aim
{
    /// <summary>
    /// Камера следует за AimPivot с задержкой. Пока угол между камерой и прицелом
    /// меньше deadzoneAngle — камера стоит. Вышел прицел за пределы — камера догоняет.
    /// Это создаёт "bodycam" ощущение: оружие водит в кадре, камера дёргается следом.
    /// </summary>
    public class CameraFollower : MonoBehaviour
    {
        [SerializeField] AimPivot aimPivot;

        [Header("Deadzone")]
        [SerializeField] float deadzoneAngle = 10f;    // градусы: свободная зона
        [SerializeField] float followSpeed = 6f;        // скорость догона
        [SerializeField] float maxFollowSpeedMultiplier = 3f;

        void LateUpdate()
        {
            if (aimPivot == null) return;

            float angle = Vector3.Angle(transform.forward, aimPivot.AimDirection);
            if (angle <= deadzoneAngle) return;

            // Чем дальше прицел от камеры — тем быстрее догоняем
            float excess = angle - deadzoneAngle;
            float speedMult = Mathf.Lerp(1f, maxFollowSpeedMultiplier, excess / 30f);
            float t = Mathf.Clamp01(followSpeed * speedMult * Time.deltaTime);

            transform.rotation = Quaternion.Slerp(transform.rotation, aimPivot.transform.rotation, t);
        }
    }
}