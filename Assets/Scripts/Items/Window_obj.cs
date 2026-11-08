using UnityEngine;

/// <summary>
/// Breakable window.
/// Can be shot out by player or Rake.
/// </summary>
public class Window_obj : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _health = 100;
    [SerializeField] private GameObject _brokenGlassPrefab;
    [SerializeField] private GameObject _intactGlass;
    [SerializeField] private AudioClip _breakSound;

    public bool IsBroken { get; private set; } = false;

    public void TakeDamage(int amount, string damageType)
    {
        if (IsBroken) return;

        _health -= amount;
        if (_health <= 0)
        {
            Break();
        }
    }

    public void SetBrokenState()
    {
        IsBroken = true;
        if (_intactGlass != null) _intactGlass.SetActive(false);
        if (_brokenGlassPrefab != null)
        {
            Instantiate(_brokenGlassPrefab, transform.position, transform.rotation, transform);
        }
    }

    private void Break()
    {
        IsBroken = true;
        if (_intactGlass != null) _intactGlass.SetActive(false);
        if (_brokenGlassPrefab != null)
        {
            Instantiate(_brokenGlassPrefab, transform.position, transform.rotation, transform);
        }
        if (_breakSound != null)
        {
            AudioSource.PlayClipAtPoint(_breakSound, transform.position);
        }
    }
}