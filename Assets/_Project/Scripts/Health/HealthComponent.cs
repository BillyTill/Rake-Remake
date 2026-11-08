using System;
using UnityEngine;
using Lake.Core;

namespace Lake.Health
{
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] bool destroyOnDeath = false;
        [SerializeField] float destroyDelay = 2f;

        public float Current { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead { get; private set; }
        public event Action<DamageInfo> OnDeath;
        public event Action<float, float> OnHealthChanged;

        void Awake() => Current = maxHealth;

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead) return;
            Current = Mathf.Max(0f, Current - info.Amount);
            OnHealthChanged?.Invoke(Current, maxHealth);
            if (Current <= 0f)
            {
                IsDead = true;
                OnDeath?.Invoke(info);
                if (destroyOnDeath) Destroy(gameObject, destroyDelay);
            }
        }

        public void ResetHealth() { IsDead = false; Current = maxHealth; OnHealthChanged?.Invoke(Current, maxHealth); }
    }
}