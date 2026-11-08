using UnityEngine;
using Lake.Core;

namespace Lake.Aim
{
    /// <summary>
    /// Невидимый прицел. Мышь вращает его напрямую.
    /// Все предметы (оружие, фонарик) — дочерние. Куда смотрит AimPivot — туда стреляют.
    /// Recoil добавляет временный offset к pitch/yaw.
    /// </summary>
    public class AimPivot : MonoBehaviour
    {
        [Header("Sensitivity")]
        [SerializeField] float baseSensitivity = 2.2f;
        [SerializeField] bool invertY = false;

        [Header("Pitch Limits")]
        [SerializeField] float minPitch = -85f;
        [SerializeField] float maxPitch = 85f;

        public Vector3 AimDirection => transform.forward;
        public Vector3 AimOrigin => transform.position;

        InputProvider _input;
        float _basePitch;
        float _baseYaw;
        Vector2 _recoilOffset; // x=pitch, y=yaw (градусы)
        float _sensitivityMultiplier = 1f;

        public void SetSensitivityMultiplier(float m) => _sensitivityMultiplier = m;
        public void AddRecoil(Vector2 pitchYawKick) => _recoilOffset += pitchYawKick;

        void Start()
        {
            _input = InputProvider.Instance;
            _baseYaw = transform.eulerAngles.y;
            _basePitch = 0f;
        }

        void Update()
        {
            if (_input == null) return;

            float sens = baseSensitivity * _sensitivityMultiplier;
            _baseYaw += _input.LookInput.x * sens;
            _basePitch += _input.LookInput.y * sens * (invertY ? 1f : -1f);

            // Decay recoil
            _recoilOffset = Vector2.Lerp(_recoilOffset, Vector2.zero, Time.deltaTime * 8f);
            if (_recoilOffset.sqrMagnitude < 0.01f) _recoilOffset = Vector2.zero;

            float finalPitch = Mathf.Clamp(_basePitch + _recoilOffset.x, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(finalPitch, _baseYaw + _recoilOffset.y, 0f);
        }
    }
}