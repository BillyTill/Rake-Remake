using UnityEngine;

namespace Lake.Core
{
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitDirection;
        public readonly GameObject Source;
        public readonly HitZoneType Zone;

        public DamageInfo(float amount, Vector3 hitPoint, Vector3 hitDirection,
                          GameObject source, HitZoneType zone = HitZoneType.Body)
        {
            Amount = amount;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            Source = source;
            Zone = zone;
        }
    }

    public enum HitZoneType { Body, Head, Limb }
}