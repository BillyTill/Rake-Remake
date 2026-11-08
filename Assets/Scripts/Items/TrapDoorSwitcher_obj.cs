using UnityEngine;

/// <summary>
/// Trap door switch - opens/closes trap doors in the environment.
/// </summary>
public class TrapDoorSwitcher_obj : DynamicObject
{
    [SerializeField] private Animator _trapDoorAnimator;
    [SerializeField] private bool _startsOpen = false;
    [SerializeField] private AudioClip _switchSound;

    private bool _isOpen = false;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        _isOpen = _startsOpen;
        if (_trapDoorAnimator != null) _trapDoorAnimator.SetBool("Open", _isOpen);
    }

    public override void Action()
    {
        ToggleTrapDoor();
    }

    public override string GetInteractionText()
    {
        return _isOpen ? "Press 'E' to close trap door" : "Press 'E' to open trap door";
    }

    private void ToggleTrapDoor()
    {
        _isOpen = !_isOpen;
        if (_trapDoorAnimator != null) _trapDoorAnimator.SetBool("Open", _isOpen);
        _audioSource?.PlayOneShot(_switchSound);
    }
}