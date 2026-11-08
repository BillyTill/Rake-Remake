using UnityEngine;

/// <summary>
/// Rake movement for multiplayer (player-controlled Rake).
/// Used when a player plays as the Rake in multiplayer mode.
/// </summary>
public class RM_RakeMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _groundMoveSpeed = 6f;
    [SerializeField] private float _groundRunSpeed = 10f;
    [SerializeField] private float _flySpeed = 3f;
    [SerializeField] private float _jumpSpeed = 8f;
    [SerializeField] private float _gravity = 20f;

    [Header("Components")]
    [SerializeField] private Animator _weaponAnimator;
    [SerializeField] private Animator _cameraAnimator;

    private CharacterController _controller;
    private Vector3 _moveDirection = Vector3.zero;
    private bool _isGrounded = false;
    private float _currentSpeed;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        // Disable if not the Rake player
        var netPlayer = GetComponent<NetworkedPlayer>();
        if (netPlayer != null && !netPlayer.IsRake)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (!IsLocalRakePlayer()) return;

        HandleMovement();
        HandleJump();
        ApplyGravity();
        UpdateAnimators();
    }

    private bool IsLocalRakePlayer()
    {
        var netPlayer = GetComponent<NetworkedPlayer>();
        return netPlayer != null && netPlayer.IsLocalPlayer && netPlayer.IsRake;
    }

    private void HandleMovement()
    {
        if (_isGrounded)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            float runInput = Input.GetAxis("Run");

            float inputMagnitude = horizontal * horizontal + vertical * vertical;
            inputMagnitude = Mathf.Clamp(inputMagnitude, 0f, 1f);
            inputMagnitude += runInput;

            if (runInput > 0.5f)
            {
                _currentSpeed = _groundRunSpeed;
            }
            else
            {
                _currentSpeed = _groundMoveSpeed;
            }

            _moveDirection = new Vector3(horizontal, 0f, vertical);
            _moveDirection = transform.TransformDirection(_moveDirection);
            _moveDirection *= _currentSpeed;

            if (_cameraAnimator != null) _cameraAnimator.SetFloat("Speed", inputMagnitude);
            if (_weaponAnimator != null)
            {
                _weaponAnimator.SetFloat("FloatSpeed", 0f);
                _weaponAnimator.SetFloat("Speed", inputMagnitude);
            }
        }
        else
        {
            // Air control (reduced)
            float horizontal = Input.GetAxis("Horizontal") * _currentSpeed * 0.5f;
            float vertical = Mathf.Clamp(Input.GetAxis("Vertical"), 0f, 1f) * _currentSpeed;
            
            _moveDirection = new Vector3(horizontal, _moveDirection.y, vertical);
            _moveDirection = transform.TransformDirection(_moveDirection);
        }
    }

    private void HandleJump()
    {
        if (Input.GetButton("Jump") && _isGrounded)
        {
            if (_weaponAnimator != null) _weaponAnimator.SetFloat("FloatSpeed", 1f);
            _moveDirection.y = _jumpSpeed;
        }
    }

    private void ApplyGravity()
    {
        if (!_isGrounded)
        {
            _moveDirection.y -= _gravity * Time.deltaTime;
        }

        _controller.Move(_moveDirection * Time.deltaTime);

        // Ground check
        bool wasGrounded = _isGrounded;
        _isGrounded = CheckGrounded();

        if (_weaponAnimator != null) _weaponAnimator.SetBool("Grounded", _isGrounded);
        if (_cameraAnimator != null) _cameraAnimator.SetBool("Grounded", _isGrounded);
    }

    private bool CheckGrounded()
    {
        RaycastHit hit;
        float rayLength = _controller.height * 0.5f + 0.1f;
        if (Physics.SphereCast(transform.position, _controller.radius, Vector3.down, out hit, rayLength))
        {
            return true;
        }
        return false;
    }

    private void UpdateAnimators()
    {
        // Additional animator parameters if needed
    }
}