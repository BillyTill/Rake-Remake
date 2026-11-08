using UnityEngine;

/// <summary>
/// Base class for all weapons and equipment.
/// WeaponID mapping:
/// 1 = Rifle (Shotgun)
/// 2 = Camera
/// 3 = Map
/// 4 = Bear Trap
/// 5 = Food (Bait)
/// 6 = Sniper Rifle
/// 7 = Binoculars
/// 8 = Medkit
/// </summary>
public abstract class Weapon : MonoBehaviour
{
    public int WeaponID;
    public int Ammo;
    public int MaxAmmo = 999;

    protected virtual void Start() { }
    protected virtual void Update() { }

    /// <summary>
    /// Called when weapon is equipped (enabled).
    /// </summary>
    public virtual void OnEquip() { }

    /// <summary>
    /// Called when weapon is unequipped (disabled).
    /// </summary>
    public virtual void OnUnequip() { }
}