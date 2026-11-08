using UnityEngine;

/// <summary>
/// Hide zone (locker, wardrobe, bush) - player can hide from Rake.
/// </summary>
public class HideZoneTrigger : MonoBehaviour
{
    [SerializeField] private bool _isLocker = true; // Locker vs bush
    [SerializeField] private Transform _hidePosition;
    [SerializeField] private Transform _exitPosition;

    private bool _playerInside = false;
    private FPSController _hiddenPlayer;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = false;
        }
    }

    private void Update()
    {
        if (_playerInside && Input.GetButtonDown("Use"))
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var fps = player.GetComponent<FPSController>();
                if (fps != null)
                {
                    if (!fps.IsHidden)
                    {
                        HidePlayer(fps);
                    }
                    else if (_hiddenPlayer == fps)
                    {
                        UnhidePlayer(fps);
                    }
                }
            }
        }
    }

    private void HidePlayer(FPSController fps)
    {
        if (_hidePosition != null)
        {
            fps.transform.position = _hidePosition.position;
            fps.transform.rotation = _hidePosition.rotation;
        }
        fps.IsHidden = true;
        _hiddenPlayer = fps;
        fps.SetPlayerState("INVENTORY"); // Disable movement
        InformationUI.Instance?.DrawHint(_isLocker ? "Hidden in locker" : "Hidden in bushes");
    }

    private void UnhidePlayer(FPSController fps)
    {
        if (_exitPosition != null)
        {
            fps.transform.position = _exitPosition.position;
            fps.transform.rotation = _exitPosition.rotation;
        }
        fps.IsHidden = false;
        _hiddenPlayer = null;
        fps.SetPlayerState("FULL");
        InformationUI.Instance?.DrawHint("Exited hiding spot");
    }
}