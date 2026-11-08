using UnityEngine;

namespace Lake.Weapons.VFX
{
    public class MuzzleFlashLight : MonoBehaviour
    {
        float _intensity;
        float _range;
        float _duration;
        float _t;
        Light _light;

        public void Setup(float intensity, float range, float duration)
        {
            _intensity = intensity;
            _range = range;
            _duration = duration;
            _light = GetComponent<Light>();
            if (_light == null) _light = gameObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = new Color(1f, 0.85f, 0.5f);
            _light.intensity = _intensity;
            _light.range = _range;
            _light.shadows = LightShadows.None;
        }

        void Update()
        {
            if (_light == null) return;
            _t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(_t / _duration);
            _light.intensity = _intensity * k;
            if (_t >= _duration) Destroy(gameObject);
        }
    }
}