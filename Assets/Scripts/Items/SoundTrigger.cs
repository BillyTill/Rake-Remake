using UnityEngine;

/// <summary>
/// Sound trigger - makes noise when player/Rake enters.
/// Used to attract Rake attention.
/// </summary>
public class SoundTrigger : MonoBehaviour
{
    public int ID; // For save/load

    [SerializeField] private AudioClip _triggerSound;
    [SerializeField] private float _cooldown = 10f;
    [SerializeField] private bool _triggerOnce = false;

    private bool _triggered = false;
    private float _lastTriggerTime = 0f;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 1f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.maxDistance = 100f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggerOnce && _triggered) return;
        if (Time.time - _lastTriggerTime < _cooldown) return;

        // Trigger on player or Rake
        if (other.CompareTag("Player") || other.CompareTag("Rake"))
        {
            Trigger();
        }
    }

    public void Trigger()
    {
        if (_audioSource != null && _triggerSound != null)
        {
            _audioSource.PlayOneShot(_triggerSound);
        }
        _lastTriggerTime = Time.time;
        _triggered = true;

        // Notify Rake of sound
        NotificationsManager.Instance.PostNotification(this, "Event_PlayerFire");
    }
}