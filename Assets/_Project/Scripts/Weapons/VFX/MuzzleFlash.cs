using UnityEngine;

namespace Lake.Weapons.VFX
{
    [RequireComponent(typeof(ParticleSystem))]
    public class MuzzleFlash : MonoBehaviour
    {
        ParticleSystem _ps;
        void Awake() { _ps = GetComponent<ParticleSystem>(); _ps.Play(); Destroy(gameObject, 0.15f); }
    }
}