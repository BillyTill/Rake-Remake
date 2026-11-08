using System.Collections;
using UnityEngine;

/// <summary>
/// Handles player movement, stamina, and footstep sounds.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _walkSpeed = 4.5f;
    [SerializeField] private float _runSpeed = 8f;
    [SerializeField] private float _jumpSpeed = 8f;
    [SerializeField] private float _gravity = 20f;

    [Header("Stamina")]
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _staminaDrainRate = 0.1f;   // Per frame when running
    [SerializeField] private float _staminaRegenRate = 0.3f;   // Per frame when not running

    [Header("Footsteps")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip[] _footstepSounds; // Different surfaces
    [SerializeField] private AudioClip _staminaExhaustedSound;
    [SerializeField] private float _walkStepInterval = 0.7f;
    [SerializeField] private float _runStepInterval = 0.4f;

    [Header("Animators")]
    [SerializeField] private Animator _bodyAnimator;
    [SerializeField] private Animator _cameraAnimator;

    private CharacterController _controller;
    private Vector3 _moveDirection = Vector3.zero;
    private float _currentStamina;
    private bool _isRunning = false;
    private bool _canRun = true;
    private bool _playStaminaSound = false;
    private bool _stepCooldown = false;
    private int _currentStepType = 0;

    public bool CanMove { get; set; } = true;
    public float CurrentStamina => _currentStamina;
    public float MaxStamina => _maxStamina;
    public bool IsRunning => _isRunning;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _currentStamina = _maxStamina;
    }

    private void Update()
    {
        if (CanMove && FPSController.Instance != null && FPSController.Instance.State == FPSController.PlayerState.Full)
        {
            HandleStamina();
            HandleMovement();
        }
        else
        {
            // Stop animations
            if (_bodyAnimator != null) _bodyAnimator.SetFloat("Speed", 0f);
            if (_cameraAnimator != null) _cameraAnimator.SetFloat("Speed", 0f);
            _isRunning = false;
        }
    }

    private void HandleStamina()
    {
        if (_isRunning)
        {
            if (_currentStamina > 1f)
            {
                _currentStamina -= _staminaDrainRate * Time.deltaTime * 60f;
            }
            else
            {
                // Stamina exhausted
                if (!_playStaminaSound)
                {
                    if (_audioSource != null && _staminaExhaustedSound != null)
                    {
                        _audioSource.PlayOneShot(_staminaExhaustedSound);
                    }
                    _playStaminaSound = true;
                }
                _canRun = false;
                _isRunning = false;
            }
        }
        else if (_currentStamina < _maxStamina)
        {
            _currentStamina += _staminaRegenRate * Time.deltaTime * 60f;
            _currentStamina = Mathf.Min(_currentStamina, _maxStamina);
        }
        else
        {
            _playStaminaSound = false;
            _canRun = true;
        }
    }

    private void HandleMovement()
    {
        if (_controller.isGrounded)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            bool hasInput = Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f;

            if (hasInput)
            {
                // Movement animations
                if (_bodyAnimator != null) _bodyAnimator.SetFloat("Speed", 1f);
                if (_cameraAnimator != null) _cameraAnimator.SetFloat("Speed", 1f);

                // Footsteps
                if (!_stepCooldown)
                {
                    float interval = _isRunning ? _runStepInterval : _walkStepInterval;
                    StartCoroutine(PlayFootstepRoutine(interval));
                }
            }
            else
            {
                if (_bodyAnimator != null) _bodyAnimator.SetFloat("Speed", 0f);
                if (_cameraAnimator != null) _cameraAnimator.SetFloat("Speed", 0f);
            }

            // Run toggle
            if (Input.GetButton("Run") && _canRun)
            {
                _isRunning = true;
            }
            else
            {
                _isRunning = false;
            }

            float speed = _isRunning ? _runSpeed : _walkSpeed;
            float stepInterval = _isRunning ? _runStepInterval : _walkStepInterval;

            // Calculate move direction
            _moveDirection = new Vector3(horizontal, 0f, vertical);
            _moveDirection = transform.TransformDirection(_moveDirection);
            _moveDirection *= speed;
        }

        // Apply gravity
        _moveDirection.y -= _gravity * Time.deltaTime;

        // Move
        _controller.Move(_moveDirection * Time.deltaTime);
    }

    private IEnumerator PlayFootstepRoutine(float interval)
    {
        _stepCooldown = true;
        if (_audioSource != null && _footstepSounds != null && _footstepSounds.Length > 0)
        {
            _audioSource.PlayOneShot(_footstepSounds[_currentStepType]);
        }
        yield return new WaitForSeconds(interval);
        _stepCooldown = false;
    }

    /// <summary>
    /// Set footstep sound type (0=grass, 1=dirt, 2=wood, 3=stone, etc.)
    /// </summary>
    public void SetFootstepSound(int type)
    {
        if (_footstepSounds == null || _footstepSounds.Length == 0) return;
        _currentStepType = Mathf.Clamp(type, 0, _footstepSounds.Length - 1);
    }

    /// <summary>
    /// Freeze player movement (used during Rake attacks).
    /// </summary>
    public void Freeze()
    {
        CanMove = false;
        _moveDirection = Vector3.zero;
    }

    /// <summary>
    /// Unfreeze player movement.
    /// </summary>
    public void Unfreeze()
    {
        CanMove = true;
    }
}