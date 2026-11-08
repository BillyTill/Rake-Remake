using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Lake.Core;

namespace Lake.BodyCam
{
    public class BodyCamController : MonoBehaviour
    {
        [SerializeField] BodyCamConfigSO config;
        [SerializeField] bool enabled_ = true;

        VolumeProfile _profile;
        LensDistortion _lens;
        ChromaticAberration _ca;
        Vignette _vig;
        FilmGrain _grain;
        ColorAdjustments _color;
        MotionBlur _motion;
        Bloom _bloom;

        public event Action<bool> OnToggled;
        public bool IsActive => enabled_;

        void Awake()
        {
            if (config == null) config = ScriptableObject.CreateInstance<BodyCamConfigSO>();
            var vol = GetComponent<Volume>() ?? gameObject.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 100f;
            vol.weight = 1f;
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.profile = _profile;

            _lens = _profile.Add<LensDistortion>(true);
            _ca = _profile.Add<ChromaticAberration>(true);
            _vig = _profile.Add<Vignette>(true);
            _grain = _profile.Add<FilmGrain>(true);
            _color = _profile.Add<ColorAdjustments>(true);
            _motion = _profile.Add<MotionBlur>(true);
            _bloom = _profile.Add<Bloom>(true);
        }

        void OnEnable()
        {
            if (InputProvider.Instance != null)
                InputProvider.Instance.BodyCamTogglePressed += Toggle;
        }

        void OnDisable()
        {
            if (InputProvider.Instance != null)
                InputProvider.Instance.BodyCamTogglePressed -= Toggle;
        }

        void Toggle()
        {
            enabled_ = !enabled_;
            OnToggled?.Invoke(enabled_);
        }

        void Update()
        {
            float k = enabled_ ? 1f : 0f;
            if (_lens != null) { _lens.intensity.Override(config.lensDistortion * k); _lens.scale.Override(Mathf.Lerp(1f, config.lensScale, k)); }
            if (_ca != null) _ca.intensity.Override(config.chromaticAberration * k);
            if (_vig != null) _vig.intensity.Override(config.vignette * k);
            if (_grain != null) _grain.intensity.Override(config.filmGrain * k);
            if (_color != null) { _color.saturation.Override(config.saturation * k); _color.contrast.Override(config.contrast * k); }
            if (_motion != null) _motion.intensity.Override(config.motionBlur * k);
            if (_bloom != null) _bloom.intensity.Override(config.bloom * k);
        }
    }
}