using UnityEngine;

/// <summary>
/// Forest zone trigger - changes ambient sounds, fog, etc when player enters.
/// </summary>
public class Forest_trigger : MonoBehaviour
{
    [SerializeField] private AudioClip _enterSound;
    [SerializeField] private AudioClip _exitSound;
    [SerializeField] private bool _changeFog = true;
    [SerializeField] private Color _fogColor = Color.gray;
    [SerializeField] private float _fogDensity = 0.02f;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _audioSource?.PlayOneShot(_enterSound);
            
            if (_changeFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = _fogColor;
                RenderSettings.fogDensity = _fogDensity;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _audioSource?.PlayOneShot(_exitSound);
            
            if (_changeFog)
            {
                RenderSettings.fog = false;
            }
        }
    }
}