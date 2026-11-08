using UnityEngine;

/// <summary>
/// Log trigger - plays sound when stepped on (attracts Rake).
/// </summary>
public class Log_trigger : MonoBehaviour
{
    [SerializeField] private AudioClip _stepSound;
    [SerializeField] private float _cooldown = 2f;

    private float _lastTriggerTime = 0f;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 1f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time - _lastTriggerTime < _cooldown) return;

        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Rake"))
        {
            _audioSource?.PlayOneShot(_stepSound);
            _lastTriggerTime = Time.time;

            // Notify Rake
            NotificationsManager.Instance.PostNotification(this, "Event_PlayerFire");
        }
    }
}