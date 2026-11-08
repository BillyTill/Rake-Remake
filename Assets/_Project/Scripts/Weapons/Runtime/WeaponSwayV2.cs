using UnityEngine;
using Lake.Core;

namespace Lake.Weapons
{
    /// <summary>
    /// Свободный sway модели при движении мыши/ходьбе.
    /// НЕ конфликтует с WeaponAnimator: sway пишется в отдельный объект-контейнер.
    /// </summary>
    public class WeaponSwayV2 : MonoBehaviour
    {
        [SerializeField] float moveSwayAmount = 0.02f;
        [SerializeField] float lookSwayAmount = 0.03f;
        [SerializeField] float swaySpeed = 6f;
        [SerializeField] float walkBobAmplitude = 0.01f;

        Vector3 _basePos;
        Quaternion _baseRot;
        InputProvider _input;
        Lake.Player.PlayerMotor _motor;
        float _walkTimer;

        void Awake() { _basePos = transform.localPosition; _baseRot = transform.localRotation; }
        void Start()
        {
            _input = InputProvider.Instance;
            _motor = GetComponentInParent<Lake.Player.PlayerMotor>();
        }

        void LateUpdate()
        {
            if (_input == null) return;

            float walk = _motor != null ? _motor.HorizontalSpeed : 0f;
            _walkTimer += Time.deltaTime * walk * 1.5f;

            Vector3 lookSway = new Vector3(
                -_input.LookInput.x * lookSwayAmount,
                -_input.LookInput.y * lookSwayAmount, 0f);

            Vector3 moveSway = new Vector3(
                -_input.MoveInput.x * moveSwayAmount, 0f, 0f);

            Vector3 walkBob = new Vector3(
                Mathf.Sin(_walkTimer) * walkBobAmplitude,
                Mathf.Sin(_walkTimer * 2f) * walkBobAmplitude, 0f);

            Vector3 target = _basePos + lookSway + moveSway + walkBob;
            transform.localPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * swaySpeed);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _baseRot, Time.deltaTime * swaySpeed);
        }
    }
}