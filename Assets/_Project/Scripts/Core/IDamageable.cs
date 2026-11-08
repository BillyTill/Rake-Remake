using System;
using UnityEngine;

namespace Lake.Core
{
    public interface IDamageable
    {
        bool IsDead { get; }
        void TakeDamage(DamageInfo info);
        event Action<DamageInfo> OnDeath;
    }

    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }
}