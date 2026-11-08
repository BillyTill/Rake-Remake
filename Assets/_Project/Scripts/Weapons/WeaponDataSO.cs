using UnityEngine;

namespace Lake.Weapons
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Lake/Weapon Data")]
    public class WeaponDataSO : ScriptableObject
    {
        [Header("Identity")]
        public WeaponType type;
        public string displayName = "Weapon";

        [Header("Firing")]
        public bool isAutomatic = true;
        public float fireRate = 0.15f;
        [Range(1, 32)] public int pelletsPerShot = 1;
        public float damagePerPellet = 25f;
        public float range = 100f;
        public LayerMask hitMask = ~0;

        [Header("Spread")]
        public float baseSpread = 0.5f;
        public float spreadPerShot = 0.3f;
        public float maxSpread = 4f;
        public float spreadRecovery = 4f;
        public float adsSpreadMultiplier = 0.2f;

        [Header("Falloff")]
        public float falloffStart = 30f;
        public float falloffEnd = 80f;
        [Range(0f, 1f)] public float minDamageMultiplier = 0.25f;

        [Header("Recoil")]
        public float recoilVertical = 1.5f;
        public float recoilHorizontal = 0.3f;
        public float visualKickback = 0.08f;

        [Header("Ammo")]
        public int magazineSize = 30;
        public int reserveAmmoMax = 120;
        public float reloadTime = 2.5f;
        public bool shellByShellReload = false;
        public float shellReloadTime = 0.5f;

        [Header("ADS")]
        public Vector3 hipPosition = new(0.22f, -0.22f, 0.55f);
        public Vector3 adsPosition = new(0f, -0.08f, 0.35f);
        public float hipFov = 65f;
        public float adsFov = 50f;
        public float adsSensitivityMultiplier = 0.55f;

        public float TotalDamage => damagePerPellet * pelletsPerShot;

        public float ComputeFalloff(float distance)
        {
            if (distance <= falloffStart) return 1f;
            if (distance >= falloffEnd) return minDamageMultiplier;
            float t = Mathf.InverseLerp(falloffStart, falloffEnd, distance);
            return Mathf.Lerp(1f, minDamageMultiplier, t);
        }
    }
}