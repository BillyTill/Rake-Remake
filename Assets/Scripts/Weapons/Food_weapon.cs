using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Food/Bait weapon (WeaponID = 5).
/// Places bait on terrain to attract boars (for Pig Cam) or lure the Rake.
/// Can only be placed on terrain.
/// </summary>
public class Food_weapon : Weapon
{
    [Header("Components")]
    [SerializeField] private Animator _animator;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _useSound;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private GameObject _baitPrefab;
    [SerializeField] private Animator _uiAnimator;
    [SerializeField] private GameObject _uiSystem;
    [SerializeField] private Transform _sightIndicator; // Shows valid placement position
    [SerializeField] private Transform _cameraTransform; // For raycast origin

    [Header("Settings")]
    [SerializeField] private float _placeDistance = 3f;

    private bool _isPlacing = false;
    private bool _canPlace = false;

    protected override void Start()
    {
        base.Start();
        WeaponID = 5;
    }

    private void OnEnable()
    {
        if (_uiSystem != null) _uiSystem.SetActive(true);
        _isPlacing = false;
        if (_sightIndicator != null) _sightIndicator.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (_uiSystem != null) _uiSystem.SetActive(false);
        if (_sightIndicator != null) _sightIndicator.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Update ammo display (handled by UI system)
        int ammo = WeaponManager.Instance.GetAmmo(WeaponID);

        // Raycast to find valid placement on terrain
        _canPlace = false;
        if (_cameraTransform != null && _sightIndicator != null)
        {
            Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, _placeDistance))
            {
                // Check if hitting terrain
                var terrainCollider = hit.collider.GetComponent<TerrainCollider>();
                if (terrainCollider != null && hit.collider.name == Terrain.activeTerrain.name)
                {
                    float terrainHeight = Terrain.activeTerrain.SampleHeight(hit.point);
                    if (hit.point.y <= terrainHeight + 0.01f)
                    {
                        _canPlace = true;
                        _sightIndicator.gameObject.SetActive(true);
                        _sightIndicator.position = hit.point;
                    }
                }
            }

            if (!_canPlace)
            {
                _sightIndicator.gameObject.SetActive(false);
            }
        }

        // Place bait
        if (Input.GetButtonDown("Fire1") && !_isPlacing && _canPlace)
        {
            if (WeaponManager.Instance.GetAmmo(WeaponID) > 0)
            {
                StartCoroutine(PlaceBaitRoutine());
            }
        }
    }

    private IEnumerator PlaceBaitRoutine()
    {
        _isPlacing = true;

        // Consume ammo
        WeaponManager.Instance.ConsumeAmmo(WeaponID, 1);

        // Animation
        if (_animator != null) _animator.SetBool("Setup", true);
        if (_audioSource != null && _useSound != null) _audioSource.PlayOneShot(_useSound);

        yield return new WaitForSeconds(0.7f);

        // Spawn bait at sight position
        if (_baitPrefab != null && _sightIndicator != null && _canPlace)
        {
            Instantiate(_baitPrefab, _sightIndicator.position, Quaternion.identity);
        }

        yield return new WaitForSeconds(0.2f);
        yield return new WaitForSeconds(0.5f);

        // Check if we have more bait
        if (WeaponManager.Instance.GetAmmo(WeaponID) > 0)
        {
            if (_animator != null) _animator.SetBool("Setup", false);
        }
        else
        {
            // Auto-remove weapon when out of ammo
            WeaponManager.Instance.RemoveWeapon(WeaponID);
            if (_uiSystem != null) _uiSystem.SetActive(false);
            gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(1f);
        _isPlacing = false;
    }
}