using UnityEngine;

/// <summary>
/// Bear trap pickup.
/// </summary>
public class Trap_item : Item
{
    public override void PickupItem()
    {
        base.PickupItem();

        if (WeaponManager.Instance != null)
        {
            if (WeaponManager.Instance.HasWeapon(4))
            {
                if (WeaponManager.Instance.IsAmmoFull(4))
                {
                    InformationUI.Instance.DrawHint("Full count of traps");
                    return;
                }
                WeaponManager.Instance.AddAmmo(4, Count);
                InformationUI.Instance.PickupInfo($"+ Bear Trap: {Count}");
            }
            else
            {
                WeaponManager.Instance.AddWeapon(4, Count, true);
                InformationUI.Instance.PickupInfo($"+ Bear Trap: {Count}");
            }
        }
    }
}