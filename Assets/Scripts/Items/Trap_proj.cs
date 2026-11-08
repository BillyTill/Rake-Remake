using UnityEngine;

/// <summary>
/// Placed bear trap in the world.
/// Damages and immobilizes the Rake when triggered.
/// </summary>
public class Trap_proj : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _damage = 50;
    [SerializeField] private float _immobilizeDuration = 7f;
    [SerializeField] private string _damageType = "TrapDamage";

    [Header("Effects")]
    [SerializeField] private GameObject _triggerEffect;
    [SerializeField] private AudioClip _triggerSound;

    private bool _triggered = false;
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        // Check if Rake
        var enemyAI = other.GetComponent<Enemy_AI>();
        if (enemyAI != null)
        {
            TriggerTrap(enemyAI);
        }

        // Check if Animal (boar)
        var animalAI = other.GetComponent<Animal_AI>();
        if (animalAI != null && !animalAI.IsTrapped)
        {
            TriggerTrap(animalAI);
        }

        // Check if Player
        var player = other.GetComponent<FPSController>();
        if (player != null)
        {
            TriggerTrap(player);
        }
    }

    private void TriggerTrap(Enemy_AI rake)
    {
        _triggered = true;
        rake.TakeDamage(_damage, _damageType);

        // Immobilize via animation/event
        rake.TriggerTrap(_immobilizeDuration);

        PlayEffects();
    }

    private void TriggerTrap(Animal_AI animal)
    {
        _triggered = true;
        animal.TakeDamage(_damage, _damageType);
        animal.SetTrappedState();

        PlayEffects();
    }

    private void TriggerTrap(FPSController player)
    {
        _triggered = true;
        player.TakeDamage(_damage / 2, _damageType); // Less damage to player

        PlayEffects();
    }

    private void PlayEffects()
    {
        if (_triggerEffect != null)
        {
            Instantiate(_triggerEffect, transform.position, transform.rotation);
        }
        if (_triggerSound != null)
        {
            AudioSource.PlayClipAtPoint(_triggerSound, transform.position);
        }
        if (_animator != null)
        {
            _animator.SetTrigger("Trigger");
        }

        // Disable collider after trigger
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Destroy after delay
        Destroy(gameObject, 30f);
    }
}