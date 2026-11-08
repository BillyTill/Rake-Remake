using UnityEngine;
using Lake.Aim;

namespace Lake.Player
{
    /// <summary>
    /// Тело игрока медленно поворачивается в сторону AimPivot.
    /// Это то, что даёт эффект "руки ведут, тело догоняет".
    /// </summary>
    public class BodyFollower : MonoBehaviour
    {
        [SerializeField] AimPivot aimPivot;
        [SerializeField] float followSpeed = 4f;

        void LateUpdate()
        {
            if (aimPivot == null) return;
            Vector3 pivotFwd = Vector3.ProjectOnPlane(aimPivot.AimDirection, Vector3.up);
            if (pivotFwd.sqrMagnitude < 0.01f) return;

            Quaternion target = Quaternion.LookRotation(pivotFwd, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * followSpeed);
        }
    }
}