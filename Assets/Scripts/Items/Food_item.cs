using UnityEngine;

/// <summary>
/// Food/Bait pickup.
/// </summary>
public class Food_item : Item
{
    public override void PickupItem()
    {
        base.PickupItem();

        if (WeaponManager.Instance != null)
        {
            if (WeaponManager.Instance.HasWeapon(5))
            {
                if (WeaponManager.Instance.IsAmmoFull(5))
                {
                    InformationUI.Instance.DrawHint("Full count of bait");
                    return;
                }
                WeaponManager.Instance.AddAmmo(5, Count);
                InformationUI.Instance.PickupInfo($"+ Bait: {Count}");
            }
            else
            {
                WeaponManager.Instance.AddWeapon(5, Count, true);
                InformationUI.Instance.PickupInfo($"+ Bait: {Count}");
            }
        }
    }
}