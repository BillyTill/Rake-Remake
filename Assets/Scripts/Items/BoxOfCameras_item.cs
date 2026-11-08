using UnityEngine;

/// <summary>
/// Box containing multiple cameras - pickup gives several cameras at once.
/// </summary>
public class BoxOfCameras_item : Item
{
    [SerializeField] private int _cameraCount = 3;

    public override void PickupItem()
    {
        base.PickupItem();

        if (WeaponManager.Instance != null)
        {
            if (WeaponManager.Instance.HasWeapon(2))
            {
                WeaponManager.Instance.AddAmmo(2, _cameraCount);
                InformationUI.Instance.PickupInfo($"+ Camera Box: {_cameraCount} cameras");
            }
            else
            {
                WeaponManager.Instance.AddWeapon(2, _cameraCount, true);
                InformationUI.Instance.PickupInfo($"+ Camera Box: {_cameraCount} cameras");
            }
        }
    }
}