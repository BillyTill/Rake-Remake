using System.Collections;
using UnityEngine;

/// <summary>
/// Animal cage trap - catches boars for Pig Cam.
/// Player places bait, boar enters, player attaches camera, releases boar.
/// </summary>
public class AnimalCage_obj : DynamicObject
{
    public enum CageState
    {
        Empty,      // No animal, bait can be placed
        Baited,     // Bait placed, waiting for animal
        Trapped,    // Animal inside, can attach camera
        CameraAttached // Camera on animal, ready to release
    }

    [Header("Components")]
    [SerializeField] private Animator _doorAnimator;
    [SerializeField] private Transform _baitPosition;
    [SerializeField] private Transform _animalPosition;
    [SerializeField] private GameObject _baitPrefab;
    [SerializeField] private AnimalRecordCamera_obj _cameraPrefab;

    [Header("Settings")]
    [SerializeField] private float _trapTime = 10f; // Time for animal to enter
    [SerializeField] private AudioClip _doorSound;
    [SerializeField] private AudioClip _trapSound;

    private CageState _state = CageState.Empty;
    private Animal_AI _trappedAnimal;
    private AudioSource _audioSource;
    private GameObject _currentBait;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
    }

    public override void Action()
    {
        switch (_state)
        {
            case CageState.Empty:
                PlaceBait();
                break;
            case CageState.Baited:
                // Wait for animal
                InformationUI.Instance?.DrawHint("Waiting for animal...");
                break;
            case CageState.Trapped:
                AttachCamera();
                break;
            case CageState.CameraAttached:
                ReleaseAnimal();
                break;
        }
    }

    public override string GetInteractionText()
    {
        switch (_state)
        {
            case CageState.Empty: return "Press 'E' to place bait";
            case CageState.Baited: return "Bait placed - waiting for boar";
            case CageState.Trapped: return "Press 'E' to attach camera";
            case CageState.CameraAttached: return "Press 'E' to release boar";
            default: return "";
        }
    }

    private void PlaceBait()
    {
        if (WeaponManager.Instance != null && WeaponManager.Instance.GetAmmo(5) > 0) // Food/Bait weaponID = 5
        {
            WeaponManager.Instance.ConsumeAmmo(5, 1);
            _currentBait = Instantiate(_baitPrefab, _baitPosition.position, _baitPosition.rotation);
            _state = CageState.Baited;
            _audioSource?.PlayOneShot(_doorSound);
            InformationUI.Instance?.PickupInfo("Bait placed. Waiting for boar...");

            StartCoroutine(WaitForAnimal());
        }
        else
        {
            InformationUI.Instance?.DrawHint("You need bait to trap a boar");
        }
    }

    private IEnumerator WaitForAnimal()
    {
        yield return new WaitForSeconds(_trapTime);

        // Find nearby boar
        var boars = FindObjectsOfType<Animal_AI>();
        foreach (var boar in boars)
        {
            if (Vector3.Distance(boar.transform.position, transform.position) < 20f && !boar.IsTrapped)
            {
                // Trap the boar
                boar.transform.position = _animalPosition.position;
                boar.transform.rotation = _animalPosition.rotation;
                boar.SetTrappedState();
                _trappedAnimal = boar;
                _state = CageState.Trapped;
                _audioSource?.PlayOneShot(_trapSound);
                
                if (_currentBait != null) Destroy(_currentBait);
                
                InformationUI.Instance?.DrawObjective("Boar trapped! Attach a camera to track the Rake.");
                yield break;
            }
        }

        // No animal came
        _state = CageState.Empty;
        if (_currentBait != null) Destroy(_currentBait);
        InformationUI.Instance?.DrawHint("No boar took the bait");
    }

    private void AttachCamera()
    {
        if (WeaponManager.Instance != null && WeaponManager.Instance.HasWeapon(2)) // Camera weaponID = 2
        {
            if (WeaponManager.Instance.GetAmmo(2) > 0)
            {
                WeaponManager.Instance.ConsumeAmmo(2, 1);
                
                // Spawn camera on animal
                var cam = Instantiate(_cameraPrefab, _trappedAnimal.transform);
                cam.transform.localPosition = Vector3.up * 1.5f;
                _trappedAnimal.SetCamera(true);

                _state = CageState.CameraAttached;
                InformationUI.Instance?.DrawObjective("Camera attached! Release the boar to track the Rake.");
            }
            else
            {
                InformationUI.Instance?.DrawHint("No cameras remaining");
            }
        }
        else
        {
            InformationUI.Instance?.DrawHint("You need a camera to attach");
        }
    }

    private void ReleaseAnimal()
    {
        if (_trappedAnimal != null)
        {
            _trappedAnimal.AfterTrapEffect();
            _trappedAnimal = null;
        }

        // Open door
        if (_doorAnimator != null) _doorAnimator.SetBool("Open", true);
        _audioSource?.PlayOneShot(_doorSound);

        _state = CageState.Empty;
        InformationUI.Instance?.DrawObjective("Boar released. Check cameras to track its movement.");
    }
}