using UnityEngine;

/// <summary>
/// Camera pickup.
/// </summary>
public class Camera_item : Item
{
    public override void PickupItem()
    {
        base.PickupItem();

        if (WeaponManager.Instance != null)
        {
            if (WeaponManager.Instance.HasWeapon(2))
            {
                if (WeaponManager.Instance.IsAmmoFull(2))
                {
                    InformationUI.Instance.DrawHint("Full count of cameras");
                    return;
                }
                WeaponManager.Instance.AddAmmo(2, Count);
                InformationUI.Instance.PickupInfo($"+ Camera: {Count}");
            }
            else
            {
                WeaponManager.Instance.AddWeapon(2, Count, true);
                InformationUI.Instance.PickupInfo($"+ Camera: {Count}");
            }
        }
    }
}