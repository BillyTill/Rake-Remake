using System.Collections;
using UnityEngine;

/// <summary>
/// Screemer system - triggers jump scare events.
/// Activated by triggers in the world.
/// </summary>
public class ScreemerSystem : MonoBehaviour
{
    public int ID; // For save/load

    [Header("Settings")]
    [SerializeField] private GameObject _screemerPrefab; // Jump scare visual/sound
    [SerializeField] private float _duration = 3f;
    [SerializeField] private bool _triggerOnce = true;

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggerOnce && _triggered) return;

        if (other.CompareTag("Player"))
        {
            TriggerScreemer();
        }
    }

    public void TriggerScreemer()
    {
        if (_triggered) return;
        _triggered = true;

        StartCoroutine(ScreemerRoutine());
    }

    private IEnumerator ScreemerRoutine()
    {
        // Disable player control
        FPSController.Instance.SetPlayerState("SCREEMER");

        // Spawn screemer
        if (_screemerPrefab != null)
        {
            Instantiate(_screemerPrefab, transform.position, transform.rotation);
        }

        yield return new WaitForSeconds(_duration);

        // Restore player control
        FPSController.Instance.SetPlayerState("FULL");
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
        if (active) _triggered = false;
    }
}