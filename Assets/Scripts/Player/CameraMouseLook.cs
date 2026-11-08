using UnityEngine;

/// <summary>
/// Handles mouse look for the player camera.
/// Can be disabled during camera mode, inventory, etc.
/// </summary>
public class CameraMouseLook : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _sensitivityX = 2f;
    [SerializeField] private float _sensitivityY = 2f;
    [SerializeField] private float _minimumY = -90f;
    [SerializeField] private float _maximumY = 90f;

    [Header("References")]
    [SerializeField] private Transform _playerBody; // For horizontal rotation

    private float _rotationY = 0f;
    public bool CanControl { get; set; } = true;

    public float SensitivityX
    {
        get => _sensitivityX;
        set => _sensitivityX = value;
    }

    public float SensitivityY
    {
        get => _sensitivityY;
        set => _sensitivityY = value;
    }

    private void Start()
    {
        if (_playerBody == null)
        {
            _playerBody = transform.root;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!CanControl) return;

        // Mouse input
        float mouseX = Input.GetAxis("Mouse X") * _sensitivityX;
        float mouseY = Input.GetAxis("Mouse Y") * _sensitivityY;

        // Horizontal rotation (player body)
        if (_playerBody != null)
        {
            _playerBody.Rotate(0f, mouseX, 0f);
        }

        // Vertical rotation (camera)
        _rotationY -= mouseY;
        _rotationY = Mathf.Clamp(_rotationY, _minimumY, _maximumY);
        transform.localEulerAngles = new Vector3(_rotationY, 0f, 0f);
    }

    /// <summary>
    /// Smoothly look at a target (used for Rake kill cam).
    /// </summary>
    public void Focus(Transform target)
    {
        if (target == null) return;

        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        Vector3 euler = lookRotation.eulerAngles;

        // Smoothly rotate player body to face target
        if (_playerBody != null)
        {
            _playerBody.rotation = Quaternion.Euler(0f, euler.y, 0f);
        }

        // Set camera vertical rotation
        _rotationY = euler.x;
        if (_rotationY > 180f) _rotationY -= 360f;
        _rotationY = Mathf.Clamp(_rotationY, _minimumY, _maximumY);
        transform.localEulerAngles = new Vector3(_rotationY, 0f, 0f);
    }
}