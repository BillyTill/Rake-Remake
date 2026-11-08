using UnityEngine;

/// <summary>
/// Rain/weather manager.
/// Minimal implementation - weather system not to be modified per user request.
/// </summary>
public class RainManager : MonoBehaviour
{
    public static RainManager Instance { get; private set; }

    [SerializeField] private ParticleSystem _rainParticles;
    [SerializeField] private Transform _defaultPlayerCamera;
    [SerializeField] private Transform _currentPlayerCamera;

    public Transform DefaultPlayerCamera => _defaultPlayerCamera;
    public Transform CurrentPlayerCamera
    {
        get => _currentPlayerCamera;
        set => _currentPlayerCamera = value;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (_defaultPlayerCamera == null)
        {
            _defaultPlayerCamera = Camera.main.transform;
        }
        _currentPlayerCamera = _defaultPlayerCamera;
    }

    private void LateUpdate()
    {
        // Follow current camera (player or camera view)
        if (_rainParticles != null && _currentPlayerCamera != null)
        {
            _rainParticles.transform.position = new Vector3(
                _currentPlayerCamera.position.x,
                _rainParticles.transform.position.y,
                _currentPlayerCamera.position.z
            );
        }
    }

    /// <summary>
    /// Set rain intensity (0-1).
    /// </summary>
    public void SetRainIntensity(float intensity)
    {
        if (_rainParticles != null)
        {
            var emission = _rainParticles.emission;
            emission.rateOverTime = intensity * 1000f;
        }
    }
}