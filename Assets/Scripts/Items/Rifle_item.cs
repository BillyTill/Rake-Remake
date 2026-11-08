using UnityEngine;

/// <summary>
/// Rifle (Shotgun) pickup.
/// </summary>
public class Rifle_item : Item
{
    public override void PickupItem()
    {
        base.PickupItem();

        if (WeaponManager.Instance != null)
        {
            if (WeaponManager.Instance.HasWeapon(1))
            {
                InformationUI.Instance.DrawHint("You already have a rifle");
                return;
            }
            WeaponManager.Instance.AddWeapon(1, Count, true);
            InformationUI.Instance.PickupInfo($"+ Rifle");
        }
    }
}