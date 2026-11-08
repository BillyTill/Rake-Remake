using UnityEngine;

/// <summary>
/// Helper component for camera setup animation/effects.
/// </summary>
public class SetupCamera : MonoBehaviour
{
    [SerializeField] private ParticleSystem _setupEffect;
    [SerializeField] private AudioClip _setupSound;

    public void PlaySetupEffect()
    {
        if (_setupEffect != null)
        {
            _setupEffect.Play();
        }
        if (_audioSource != null && _setupSound != null)
        {
            _audioSource.PlayOneShot(_setupSound);
        }
    }

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
    }
}