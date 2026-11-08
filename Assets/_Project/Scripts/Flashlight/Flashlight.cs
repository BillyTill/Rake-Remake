using System;
using UnityEngine;
using Lake.Core;

namespace Lake.Flashlight
{
    [RequireComponent(typeof(Light))]
    public class Flashlight : MonoBehaviour
    {
        [SerializeField] bool useBattery = false;
        [SerializeField] LinearDrainBattery battery = new();
        [SerializeField] float onIntensity = 5f;
        [SerializeField] float onRange = 50f;
        [SerializeField] float onSpotAngle = 45f;
        [SerializeField] Color lightColor = new(1f, 0.95f, 0.85f);
        [SerializeField] bool enableFlicker = true;
        [SerializeField] float flickerAmplitude = 0.1f;
        [SerializeField] float flickerSpeed = 8f;

        Light _light;
        InputProvider _input;
        bool _isOn;
        float _phase;
        float _lastBatteryNotify = -1f;

        public event Action<bool> OnToggled;
        public event Action<float> OnBatteryChanged;

        public bool IsOn => _isOn;
        public IBatteryStrategy CurrentBattery => Battery;

        IBatteryStrategy Battery => useBattery ? battery : new InfiniteBattery();

        void Awake()
        {
            _light = GetComponent<Light>();
            _light.type = LightType.Spot;
            _light.color = lightColor;
            _light.range = onRange;
            _light.spotAngle = onSpotAngle;
            _light.shadows = LightShadows.Soft;
            _light.enabled = false;
        }

        void OnEnable()
        {
            if (InputProvider.Instance != null)
                InputProvider.Instance.FlashlightPressed += Toggle;
            _phase = UnityEngine.Random.value * 100f;
        }

        void OnDisable()
        {
            if (InputProvider.Instance != null)
                InputProvider.Instance.FlashlightPressed -= Toggle;
        }

        void Toggle() => SetOn(!_isOn);

        public void SetOn(bool on)
        {
            if (on && !Battery.CanUse) return;
            if (_isOn == on) return;
            _isOn = on;
            _light.enabled = on;
            OnToggled?.Invoke(on);
        }

        void Update()
        {
            Battery.Tick(Time.deltaTime, _isOn);
            if (_isOn && !Battery.CanUse) { SetOn(false); return; }
            if (!_isOn) return;

            if (!enableFlicker) { _light.intensity = onIntensity; return; }

            _phase += Time.deltaTime * flickerSpeed;
            float n = Mathf.PerlinNoise(_phase, 0f) - 0.5f;
            float level = useBattery ? Mathf.Lerp(0.4f, 1f, Battery.CurrentLevel) : 1f;
            _light.intensity = onIntensity * level * (1f + n * 2f * flickerAmplitude);

            // Notify battery level every 0.2s
            if (Time.time - _lastBatteryNotify >= 0.2f)
            {
                _lastBatteryNotify = Time.time;
                OnBatteryChanged?.Invoke(Battery.CurrentLevel);
            }
        }
    }
}