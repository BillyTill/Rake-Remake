using UnityEngine;
using Lake.Core;

namespace Lake.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;
        [SerializeField] float walkSpeed = 4f;
        [SerializeField] float sprintSpeed = 7f;
        [SerializeField] float crouchSpeed = 2.2f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float jumpHeight = 1.2f;
        [SerializeField] float crouchHeight = 1f;
        [SerializeField] float standHeight = 1.8f;
        [SerializeField] float crouchLerpSpeed = 10f;

        CharacterController _cc;
        InputProvider _input;
        Vector3 _velocity;
        float _targetHeight;

        public bool IsGrounded => _cc.isGrounded;
        public float HorizontalSpeed { get; private set; }

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _cc.height = standHeight;
            _cc.center = new Vector3(0f, standHeight * 0.5f, 0f);
            _targetHeight = standHeight;
        }

        void Start() => _input = InputProvider.Instance;

        void Update()
        {
            if (_input == null) return;
            HandleCrouch();
            HandleMovement();
            HandleGravity();
            _cc.Move(_velocity * Time.deltaTime);
        }

        void HandleMovement()
        {
            // Движение относительно камеры (вид игрока)
            Vector3 camFwd = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            Vector2 move = _input.MoveInput;
            Vector3 dir = (camRight * move.x + camFwd * move.y);

            float speed = _input.Crouch ? crouchSpeed : (_input.Sprint ? sprintSpeed : walkSpeed);
            HorizontalSpeed = dir.magnitude * speed;
            _velocity.x = dir.x * speed;
            _velocity.z = dir.z * speed;
        }

        void HandleGravity()
        {
            if (_cc.isGrounded && _velocity.y < 0f) _velocity.y = -2f;
            else if (!_cc.isGrounded) _velocity.y += gravity * Time.deltaTime;

            if (_cc.isGrounded && Input.GetKeyDown(KeyCode.Space))
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        void HandleCrouch()
        {
            _targetHeight = _input.Crouch ? crouchHeight : standHeight;
            _cc.height = Mathf.Lerp(_cc.height, _targetHeight, Time.deltaTime * crouchLerpSpeed);
            _cc.center = new Vector3(0f, _cc.height * 0.5f, 0f);
        }
    }
}