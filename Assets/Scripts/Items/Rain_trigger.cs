using UnityEngine;

/// <summary>
/// Rain zone trigger - activates rain when player enters.
/// </summary>
public class Rain_trigger : MonoBehaviour
{
    [SerializeField] private float _fadeInTime = 2f;
    [SerializeField] private float _fadeOutTime = 2f;
    [SerializeField] private float _targetIntensity = 1f;

    private bool _playerInside = false;
    private float _currentIntensity = 0f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = false;
        }
    }

    private void Update()
    {
        float target = _playerInside ? _targetIntensity : 0f;
        float speed = _playerInside ? (1f / _fadeInTime) : (1f / _fadeOutTime);
        
        _currentIntensity = Mathf.MoveTowards(_currentIntensity, target, speed * Time.deltaTime);
        
        if (RainManager.Instance != null)
        {
            RainManager.Instance.SetRainIntensity(_currentIntensity);
        }
    }
}