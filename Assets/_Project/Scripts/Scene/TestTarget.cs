using UnityEngine;
using Lake.Health;
using Lake.Core;

namespace Lake.Scene
{
    [RequireComponent(typeof(HealthComponent))]
    public class TestTarget : MonoBehaviour
    {
        [SerializeField] Renderer targetRenderer;
        [SerializeField] Color hitColor = Color.red;
        HealthComponent _hc;
        MaterialPropertyBlock _mpb;

        void Awake()
        {
            _hc = GetComponent<HealthComponent>();
            _mpb = new MaterialPropertyBlock();
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        }

        void Start()
        {
            _hc.OnHealthChanged += (_, __) => Flash(hitColor);
            _hc.OnDeath += _ => Flash(Color.black);
        }

        void Flash(Color c)
        {
            if (targetRenderer == null) return;
            targetRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", c);
            targetRenderer.SetPropertyBlock(_mpb);
        }
    }
}