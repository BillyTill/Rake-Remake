using UnityEngine;

/// <summary>
/// Auto-destroy object after specified time.
/// </summary>
public class AutoDestroy : MonoBehaviour
{
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private bool _destroyOnCollision = false;
    [SerializeField] private string[] _collisionTags;

    private void Start()
    {
        if (_lifeTime > 0)
        {
            Destroy(gameObject, _lifeTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_destroyOnCollision) return;
        if (_collisionTags == null || _collisionTags.Length == 0) return;

        foreach (var tag in _collisionTags)
        {
            if (collision.gameObject.CompareTag(tag))
            {
                Destroy(gameObject);
                break;
            }
        }
    }
}