using System.Collections;
using UnityEngine;

/// <summary>
/// Computer in the van used to monitor cameras.
/// Player sits at this computer to switch between camera views.
/// </summary>
public class CameraComputer : DynamicObject
{
    [Header("Components")]
    [SerializeField] private Animator _effectsAnimator;
    [SerializeField] private GameObject _cameraUI;              // Camera monitor UI
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioSource _motionAudio;          // Static noise when rotating
    [SerializeField] private AudioClip _startSound;

    [Header("Settings")]
    [SerializeField] private float _rotationSpeed = 90f;
    [SerializeField] private float _smoothTime = 0.1f;

    private bool _isProcessing = false;
    public bool IsWatching { get; private set; } = false;

    private Quaternion _targetRotation = Quaternion.identity;
    private float _rotationY = 0f;

    private void Start()
    {
        if (_cameraUI != null) _cameraUI.SetActive(false);
        if (_motionAudio != null) _motionAudio.mute = true;
    }

    private void Update()
    {
        if (IsWatching)
        {
            // Exit on E key
            if (Input.GetKeyDown(KeyCode.E))
            {
                StartCoroutine(ChangeMode(true));
            }

            // Rotate camera with mouse/keyboard
            float horizontal = Input.GetAxis("Horizontal");
            if (Mathf.Abs(horizontal) > 0.01f)
            {
                _rotationY += horizontal * _rotationSpeed * Time.deltaTime;
                _rotationY = Mathf.Clamp(_rotationY, -45f, 45f);

                // Apply rotation to current camera
                var currentCam = CameraManager.Instance.CurrentCamera;
                if (currentCam != null)
                {
                    currentCam.localEulerAngles = new Vector3(
                        transform.localEulerAngles.x,
                        _rotationY,
                        0f
                    );
                }

                // Motion static sound volume based on rotation speed
                if (_motionAudio != null)
                {
                    _motionAudio.volume = Mathf.Abs(horizontal);
                }
            }
        }
    }

    /// <summary>
    /// Player interacts with computer (E key).
    /// </summary>
    public override void Action()
    {
        if (!_isProcessing)
        {
            if (CameraManager.Instance.CameraCount > 0)
            {
                StartCoroutine(ChangeMode(false));
            }
            else
            {
                InformationUI.Instance.DrawHint("You did not setup cameras");
            }
        }
    }

    /// <summary>
    /// Exit camera mode.
    /// </summary>
    public void Exit()
    {
        StartCoroutine(ChangeMode(true));
    }

    /// <summary>
    /// Reset camera rotation when switching cameras.
    /// </summary>
    public void ResetCameraPosition()
    {
        _rotationY = 0f;
        StartCoroutine(SwitchEffect());
    }

    private IEnumerator SwitchEffect()
    {
        if (_effectsAnimator != null)
        {
            _effectsAnimator.SetBool("Switch", true);
            yield return new WaitForSeconds(0.2f);
            _effectsAnimator.SetBool("Switch", false);
        }
    }

    /// <summary>
    /// Switch between player view and camera view.
    /// </summary>
    /// <param name="exit">True to exit camera mode, false to enter</param>
    private IEnumerator ChangeMode(bool exit)
    {
        if (!exit)
        {
            // Enter camera mode
            if (_audioSource != null && _startSound != null)
                _audioSource.PlayOneShot(_startSound);

            if (_effectsAnimator != null)
                _effectsAnimator.SetBool("Fade", true);

            IsWatching = false;
            _isProcessing = true;

            yield return new WaitForSeconds(1f);

            // Disable player controls
            if (FPSController.Instance != null)
            {
                FPSController.Instance.DisablePlayer();
            }

            if (_effectsAnimator != null)
                _effectsAnimator.SetBool("Fade", false);

            // Activate first camera
            CameraManager.Instance.ActivateCamera(0);

            yield return new WaitForSeconds(1f);

            // Show UI
            if (_cameraUI != null) _cameraUI.SetActive(true);

            IsWatching = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_motionAudio != null) _motionAudio.mute = false;

            _isProcessing = false;
        }
        else
        {
            // Exit camera mode
            if (_audioSource != null && _startSound != null)
                _audioSource.PlayOneShot(_startSound);

            if (_effectsAnimator != null)
                _effectsAnimator.SetBool("Fade", true);

            IsWatching = false;
            if (_motionAudio != null) _motionAudio.mute = true;
            _isProcessing = true;

            yield return new WaitForSeconds(1f);

            // Enable player controls
            if (FPSController.Instance != null)
            {
                FPSController.Instance.EnablePlayer();
            }

            if (_effectsAnimator != null)
                _effectsAnimator.SetBool("Fade", false);

            // Disable all cameras
            CameraManager.Instance.DisableAllCameras();

            if (_cameraUI != null) _cameraUI.SetActive(false);

            yield return new WaitForSeconds(1f);

            // Reset rain camera
            if (RainManager.Instance != null)
            {
                RainManager.Instance.CurrentPlayerCamera = RainManager.Instance.DefaultPlayerCamera;
            }

            IsWatching = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _isProcessing = false;
        }
    }
}