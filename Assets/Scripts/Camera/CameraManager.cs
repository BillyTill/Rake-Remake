using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages all active cameras (max 5).
/// Handles switching, UI buttons, and integration with CameraComputer.
/// </summary>
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Camera Lists")]
    [SerializeField] private List<Transform> _gameCameras = new List<Transform>();    // Active cameras (max 5)
    [SerializeField] private List<Transform> _allCameras = new List<Transform>();     // All camera mounts in level

    [Header("UI")]
    [SerializeField] private List<Button> _cameraButtons = new List<Button>();        // UI buttons for switching
    [SerializeField] private AudioClip _cameraSelectSound;
    [SerializeField] private AudioSource _audioSource;

    [Header("References")]
    [SerializeField] private CameraComputer _cameraComputer;

    private int _currentCameraID = 0;
    private Transform _currentCameraTransform;

    // Properties
    public int CameraCount => _gameCameras.Count;
    public int AllCamerasCount => _allCameras.Count;
    public int CurrentCameraID => _currentCameraID;
    public Transform CurrentCamera => _currentCameraTransform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Ensure we have 5 button slots
        while (_cameraButtons.Count < 5)
        {
            _cameraButtons.Add(null);
        }
    }

    /// <summary>
    /// Get camera at index in active list.
    /// </summary>
    public Transform GetCameraAtIndex(int index)
    {
        if (index >= 0 && index < _gameCameras.Count)
            return _gameCameras[index];
        return null;
    }

    /// <summary>
    /// Get camera mount at index in all cameras list.
    /// </summary>
    public Transform GetAllCameraAtIndex(int index)
    {
        if (index >= 0 && index < _allCameras.Count)
            return _allCameras[index];
        return null;
    }

    /// <summary>
    /// Get currently active camera.
    /// </summary>
    public Transform GetCurrentCamera()
    {
        if (_currentCameraID >= 0 && _currentCameraID < _gameCameras.Count)
            return _gameCameras[_currentCameraID];
        return null;
    }

    /// <summary>
    /// Add a camera to active list.
    /// </summary>
    public void AddCamera(Transform camera)
    {
        if (camera != null && !_gameCameras.Contains(camera))
        {
            _gameCameras.Add(camera);
            RefreshButtons();
        }
    }

    /// <summary>
    /// Remove a camera from active list.
    /// </summary>
    public void RemoveCamera(Transform camera)
    {
        _gameCameras.Remove(camera);
        RefreshButtons();

        // If we were watching this camera, switch to another or exit
        if (_cameraComputer != null && _cameraComputer.IsWatching)
        {
            if (_gameCameras.Count > 0)
            {
                ActivateCamera(0);
            }
            else
            {
                _cameraComputer.Exit();
            }
        }
    }

    /// <summary>
    /// Clear all active cameras (used on load).
    /// </summary>
    public void ClearCameras()
    {
        _gameCameras.Clear();
        RefreshButtons();
    }

    /// <summary>
    /// Find index of camera in active list.
    /// </summary>
    public int FindCameraIndex(Transform camera)
    {
        for (int i = 0; i < _gameCameras.Count; i++)
        {
            if (_gameCameras[i] == camera)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Activate a specific camera by index.
    /// </summary>
    public void ActivateCamera(int index)
    {
        for (int i = 0; i < _gameCameras.Count; i++)
        {
            if (_gameCameras[i] != null)
            {
                // Get the actual camera component (first child)
                var camObj = _gameCameras[i].gameObject;
                bool isActive = (i == index);
                camObj.SetActive(isActive);

                if (isActive)
                {
                    _currentCameraID = index;
                    _currentCameraTransform = _gameCameras[i];

                    // Notify camera computer
                    if (_cameraComputer != null)
                    {
                        _cameraComputer.ResetCameraPosition();
                    }

                    // Mark as recording
                    var recordCam = _gameCameras[i].GetComponentInParent<RecordCamera_obj>();
                    if (recordCam != null)
                    {
                        recordCam.IsRecording = true;
                    }

                    // Play sound
                    if (_audioSource != null && _cameraSelectSound != null)
                    {
                        _audioSource.PlayOneShot(_cameraSelectSound);
                    }

                    // Update rain manager
                    if (RainManager.Instance != null)
                    {
                        RainManager.Instance.CurrentPlayerCamera = _currentCameraTransform;
                    }
                }
            }
        }
        RefreshButtons();
    }

    /// <summary>
    /// Disable all camera views.
    /// </summary>
    public void DisableAllCameras()
    {
        for (int i = 0; i < _gameCameras.Count; i++)
        {
            if (_gameCameras[i] != null)
            {
                var camObj = _gameCameras[i].gameObject;
                camObj.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Called when camera button is pressed (highlight).
    /// </summary>
    public void OnCameraButtonEnter(int camIndex)
    {
        if (camIndex >= 0 && camIndex < _cameraButtons.Count && _cameraButtons[camIndex] != null)
        {
            var colors = _cameraButtons[camIndex].colors;
            colors.normalColor = Color.red;
            _cameraButtons[camIndex].colors = colors;
        }
    }

    /// <summary>
    /// Called when camera button is released.
    /// </summary>
    public void OnCameraButtonExit(int camIndex)
    {
        if (camIndex >= 0 && camIndex < _cameraButtons.Count && _cameraButtons[camIndex] != null)
        {
            var colors = _cameraButtons[camIndex].colors;
            colors.normalColor = Color.white;
            _cameraButtons[camIndex].colors = colors;
        }
    }

    /// <summary>
    /// Register a camera mount in the level.
    /// </summary>
    public void RegisterCameraMount(Transform mount)
    {
        if (!_allCameras.Contains(mount))
        {
            _allCameras.Add(mount);
        }
    }

    /// <summary>
    /// Fire notification (when player shoots).
    /// </summary>
    public void OnPlayerFire()
    {
        NotificationsManager.Instance.PostNotification(this, "Event_PlayerFire");
    }

    /// <summary>
    /// Update button interactable states.
    /// </summary>
    private void RefreshButtons()
    {
        for (int i = 0; i < _cameraButtons.Count; i++)
        {
            if (_cameraButtons[i] != null)
            {
                _cameraButtons[i].interactable = (i < _gameCameras.Count);
            }
        }
    }
}