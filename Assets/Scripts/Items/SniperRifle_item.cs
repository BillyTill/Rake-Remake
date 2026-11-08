using UnityEngine;

/// <summary>
/// Sniper Rifle pickup.
/// </summary>
public class SniperRifle_item : Item
{
    public override void PickupItem()
    {
        base.PickupItem();

        if (WeaponManager.Instance != null)
        {
            if (WeaponManager.Instance.HasWeapon(6))
            {
                InformationUI.Instance.DrawHint("You already have a sniper rifle");
                return;
            }
            WeaponManager.Instance.AddWeapon(6, Count, true);
            InformationUI.Instance.PickupInfo($"+ Sniper Rifle");
        }
    }
}