using UnityEngine;
using Lake.Core;
using Lake.Aim;

namespace Lake.Weapons
{
    public class ADSController : MonoBehaviour
    {
        [SerializeField] WeaponController weaponController;
        [SerializeField] AimPivot aimPivot;
        [SerializeField] Camera playerCamera;

        bool _ads;
        float _t;

        void OnEnable()
        {
            if (InputProvider.Instance == null) return;
            InputProvider.Instance.ADSStarted += OnADSStart;
            InputProvider.Instance.ADSEnded += OnADSEnd;
        }

        void OnDisable()
        {
            if (InputProvider.Instance == null) return;
            InputProvider.Instance.ADSStarted -= OnADSStart;
            InputProvider.Instance.ADSEnded -= OnADSEnd;
        }

        void OnADSStart() => SetADS(true);
        void OnADSEnd() => SetADS(false);

        void SetADS(bool ads)
        {
            _ads = ads;
            // Уведомить текущий аниматор (находится в дочернем AnimContainer)
            var current = weaponController.Current;
            if (current != null)
            {
                var animator = current.GetComponentInChildren<WeaponAnimator>();
                animator?.SetADS(ads);
            }
        }

        void Update()
        {
            var w = weaponController.Current;
            if (w == null || w.Data == null) return;

            _t = Mathf.MoveTowards(_t, _ads ? 1f : 0f, Time.deltaTime * 10f);

            if (playerCamera != null)
                playerCamera.fieldOfView = Mathf.Lerp(w.Data.hipFov, w.Data.adsFov, _t);

            aimPivot.SetSensitivityMultiplier(
                Mathf.Lerp(1f, w.Data.adsSensitivityMultiplier, _t));
        }
    }
}