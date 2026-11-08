using UnityEngine;

/// <summary>
/// Rifle ammo pickup.
/// </summary>
public class RifleAmmo_item : Item
{
    public override void PickupItem()
    {
        base.PickupItem();

        if (WeaponManager.Instance != null)
        {
            if (WeaponManager.Instance.HasWeapon(1))
            {
                if (WeaponManager.Instance.IsAmmoFull(1))
                {
                    InformationUI.Instance.DrawHint("Full rifle ammo");
                    return;
                }
                WeaponManager.Instance.AddAmmo(1, Count);
                InformationUI.Instance.PickupInfo($"+ Rifle ammo: {Count}");
            }
            else
            {
                WeaponManager.Instance.AddWeapon(1, Count, true);
                InformationUI.Instance.PickupInfo($"+ Rifle with {Count} ammo");
            }
        }
    }
}