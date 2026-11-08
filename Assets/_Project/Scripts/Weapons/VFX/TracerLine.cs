using UnityEngine;

namespace Lake.Weapons.VFX
{
    [RequireComponent(typeof(LineRenderer))]
    public class TracerLine : MonoBehaviour
    {
        LineRenderer _lr;
        void Awake() => _lr = GetComponent<LineRenderer>();
        public void SetPosition(int i, Vector3 p) => _lr.SetPosition(i, p);
    }
}