using System;
using UnityEngine;

namespace Lake.Flashlight
{
    public interface IBatteryStrategy
    {
        float CurrentLevel { get; }
        bool CanUse { get; }
        void Tick(float dt, bool isOn);
        void Reset();
    }

    [Serializable]
    public class InfiniteBattery : IBatteryStrategy
    {
        public float CurrentLevel => 1f;
        public bool CanUse => true;
        public void Tick(float dt, bool isOn) { }
        public void Reset() { }
    }

    [Serializable]
    public class LinearDrainBattery : IBatteryStrategy
    {
        [SerializeField] float drainPerSecond = 0.5f;
        [SerializeField] float rechargePerSecond = 1f;

        float _level = 1f;
        public float CurrentLevel => _level;
        public bool CanUse => _level > 0f;

        public void Tick(float dt, bool isOn)
        {
            if (isOn) _level = Mathf.Max(0f, _level - drainPerSecond * dt);
            else _level = Mathf.Min(1f, _level + rechargePerSecond * dt);
        }

        public void Reset() => _level = 1f;
    }
}