using UnityEngine;
using Lake.Core;

namespace Lake.Aim
{
    /// <summary>
    /// Два канала отдачи:
    /// 1) AimPivot — реальный подброс прицела (влияет на попадание)
    /// 2) Weapon visual — визуальный kickback модели (не влияет на попадание)
    /// </summary>
    public class RecoilController : MonoBehaviour
    {
        [SerializeField] AimPivot aimPivot;

        public void ApplyRecoil(float vertical, float horizontal)
        {
            float h = Random.Range(-horizontal, horizontal);
            aimPivot.AddRecoil(new Vector2(vertical, h));
        }
    }
}