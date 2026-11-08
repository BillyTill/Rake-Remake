using UnityEngine;

/// <summary>
/// Dead Rake object - spawned when Rake is killed.
/// </summary>
public class RakeDead_obj : MonoBehaviour
{
    [SerializeField] private float _despawnTime = 300f; // 5 minutes
    [SerializeField] private GameObject _lootDrop;

    private void Start()
    {
        // Drop loot
        if (_lootDrop != null)
        {
            Instantiate(_lootDrop, transform.position + Vector3.up, Quaternion.identity);
        }

        // Despawn after time
        Destroy(gameObject, _despawnTime);
    }
}