using UnityEngine;

/// <summary>
/// Medkit pickup.
/// </summary>
public class Medkit_item : Item
{
    public override void PickupItem()
    {
        base.PickupItem();

        if (WeaponManager.Instance != null)
        {
            if (WeaponManager.Instance.HasWeapon(8))
            {
                if (WeaponManager.Instance.IsAmmoFull(8))
                {
                    InformationUI.Instance.DrawHint("Full count of medkits");
                    return;
                }
                WeaponManager.Instance.AddAmmo(8, Count);
                InformationUI.Instance.PickupInfo($"+ Medkit: {Count}");
            }
            else
            {
                WeaponManager.Instance.AddWeapon(8, Count, true);
                InformationUI.Instance.PickupInfo($"+ Medkit: {Count}");
            }
        }
    }
}