using UnityEngine;

namespace Lake.Weapons
{
    /// <summary>
    /// Анимация отдачи модели: kickback по Z, muzzle flip по X, случайный yaw.
    /// Возвращает модель в исходное положение через пружину (Spring-Damper).
    /// </summary>
    public class WeaponAnimator : MonoBehaviour
    {
        [SerializeField] Transform weaponModel;
        [SerializeField] Vector3 hipPosition = new(0.22f, -0.22f, 0.55f);
        [SerializeField] Vector3 adsPosition = new(0f, -0.08f, 0.35f);
        [SerializeField] float kickback = 0.08f;
        [SerializeField] float muzzleFlip = 8f;
        [SerializeField] float recoilReturnSpeed = 12f;

        Vector3 _currentPos;
        Quaternion _currentRot;
        Vector3 _targetPos;
        Quaternion _targetRot;
        Vector3 _recoilPos;
        float _recoilPitch;
        float _recoilYaw;
        Vector3 _velocity;
        Vector3 _angularVelocity;

        float _adsT;
        bool _isAds;

        public void SetADS(bool ads) => _isAds = ads;

        public void ApplyKick()
        {
            _recoilPos.z -= kickback;
            _recoilPitch -= muzzleFlip;
            _recoilYaw += Random.Range(-2f, 2f);
        }

        void LateUpdate()
        {
            if (weaponModel == null) return;

            // ADS transition
            _adsT = Mathf.MoveTowards(_adsT, _isAds ? 1f : 0f, Time.deltaTime * 10f);
            Vector3 basePos = Vector3.Lerp(hipPosition, adsPosition, _adsT);

            // Spring return recoil
            _recoilPos = Vector3.SmoothDamp(_recoilPos, Vector3.zero, ref _velocity, 1f / recoilReturnSpeed);
            _recoilPitch = Mathf.SmoothDamp(_recoilPitch, 0f, ref _angularVelocity.x, 1f / recoilReturnSpeed);
            _recoilYaw = Mathf.SmoothDamp(_recoilYaw, 0f, ref _angularVelocity.y, 1f / recoilReturnSpeed);

            weaponModel.localPosition = basePos + _recoilPos;
            weaponModel.localRotation = Quaternion.Euler(_recoilPitch, _recoilYaw, 0f);
        }
    }
}