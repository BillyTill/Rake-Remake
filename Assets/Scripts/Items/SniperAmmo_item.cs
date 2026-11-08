using UnityEngine;

/// <summary>
/// Sniper rifle ammo pickup.
/// </summary>
public class SniperAmmo_item : Item
{
    public override void PickupItem()
    {
        base.PickupItem();

        if (WeaponManager.Instance != null)
        {
            if (WeaponManager.Instance.HasWeapon(6))
            {
                if (WeaponManager.Instance.IsAmmoFull(6))
                {
                    InformationUI.Instance.DrawHint("Full sniper ammo");
                    return;
                }
                WeaponManager.Instance.AddAmmo(6, Count);
                InformationUI.Instance.PickupInfo($"+ Sniper ammo: {Count}");
            }
            else
            {
                WeaponManager.Instance.AddWeapon(6, Count, true);
                InformationUI.Instance.PickupInfo($"+ Sniper Rifle with {Count} ammo");
            }
        }
    }
}