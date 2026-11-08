using UnityEngine;
using Lake.Core;

namespace Lake.Player
{
    /// <summary>
    /// Head bob + tilt. Пишет в CameraContainer (родитель камеры), НЕ в AimPivot.
    /// </summary>
    public class CameraEffects : MonoBehaviour
    {
        [SerializeField] Transform cameraContainer;
        [SerializeField] PlayerMotor motor;
        [SerializeField] float bobFrequency = 8f;
        [SerializeField] float bobAmplitudeX = 0.025f;
        [SerializeField] float bobAmplitudeY = 0.04f;
        [SerializeField] float tiltAmount = 2f;

        InputProvider _input;
        Vector3 _baseLocalPos;
        Vector3 _bobOffset;
        float _bobTimer;
        float _tilt;

        void Start()
        {
            _input = InputProvider.Instance;
            _baseLocalPos = cameraContainer.localPosition;
        }

        void LateUpdate()
        {
            if (_input == null || motor == null) return;

            bool moving = motor.IsGrounded && motor.HorizontalSpeed > 0.1f;
            if (moving)
            {
                _bobTimer += Time.deltaTime * bobFrequency;
                _bobOffset = Vector3.Lerp(_bobOffset,
                    new Vector3(Mathf.Sin(_bobTimer) * bobAmplitudeX,
                                Mathf.Sin(_bobTimer * 2f) * bobAmplitudeY, 0f),
                    Time.deltaTime * 12f);
            }
            else _bobOffset = Vector3.Lerp(_bobOffset, Vector3.zero, Time.deltaTime * 10f);

            float targetTilt = -_input.MoveInput.x * tiltAmount;
            _tilt = Mathf.Lerp(_tilt, targetTilt, Time.deltaTime * 8f);

            cameraContainer.localPosition = _baseLocalPos + _bobOffset;
            cameraContainer.localRotation = Quaternion.Euler(0f, 0f, _tilt);
        }
    }
}