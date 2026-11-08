using System.Collections;
using UnityEngine;

/// <summary>
/// Interactive door - can be opened/closed.
/// Rake can open doors in safe zones.
/// </summary>
public class Door_obj : DynamicObject
{
    [SerializeField] private Animator _animator;
    [SerializeField] private AudioClip _openSound;
    [SerializeField] private AudioClip _closeSound;
    [SerializeField] private bool _startsOpen = false;
    [SerializeField] private bool _autoClose = false;
    [SerializeField] private float _autoCloseDelay = 3f;

    private bool _isOpen = false;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        _isOpen = _startsOpen;
        if (_animator != null) _animator.SetBool("Open", _isOpen);
    }

    public override void Action()
    {
        ToggleDoor();
    }

    public override string GetInteractionText()
    {
        return _isOpen ? "Press 'E' to close door" : "Press 'E' to open door";
    }

    public void ToggleDoor()
    {
        _isOpen = !_isOpen;
        if (_animator != null) _animator.SetBool("Open", _isOpen);

        if (_isOpen)
        {
            _audioSource?.PlayOneShot(_openSound);
        }
        else
        {
            _audioSource?.PlayOneShot(_closeSound);
        }

        if (_autoClose && _isOpen)
        {
            StartCoroutine(AutoCloseRoutine());
        }
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(_autoCloseDelay);
        if (_isOpen)
        {
            ToggleDoor();
        }
    }

    /// <summary>
    /// Called by Rake AI to open door.
    /// </summary>
    public void ForceOpen()
    {
        if (!_isOpen)
        {
            ToggleDoor();
        }
    }

    /// <summary>
    /// Called by Rake AI to close door.
    /// </summary>
    public void ForceClose()
    {
        if (_isOpen)
        {
            ToggleDoor();
        }
    }
}