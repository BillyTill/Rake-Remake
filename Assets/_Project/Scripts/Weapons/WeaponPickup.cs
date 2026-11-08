using UnityEngine;
using Lake.Core;

namespace Lake.Weapons
{
    public class WeaponPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] WeaponType type;
        [SerializeField] WeaponDataSO data;
        [SerializeField] GameObject weaponPrefab;
        [SerializeField] string displayName = "Weapon";

        public string Prompt => $"E - Pick up {displayName}";

        public bool CanInteract(GameObject interactor) =>
            interactor.GetComponentInParent<WeaponController>() != null;

        public void Interact(GameObject interactor)
        {
            var wc = interactor.GetComponentInParent<WeaponController>();
            if (wc == null) return;
            var prefab = weaponPrefab != null ? weaponPrefab : gameObject;
            if (wc.TryAddWeapon(type, prefab, data)) Destroy(gameObject);
        }

        public void Setup(WeaponType t, WeaponDataSO d)
        {
            type = t;
            data = d;
            displayName = d != null ? d.displayName : t.ToString();
            weaponPrefab = null; // drop-режим: gameObject сам является префабом-инстансом
        }
    }
}