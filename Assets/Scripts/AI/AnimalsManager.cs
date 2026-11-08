using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all animals in the level (spawning, population control).
/// </summary>
public class AnimalsManager : MonoBehaviour
{
    public static AnimalsManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private GameObject _boarPrefab;
    [SerializeField] private GameObject _deerPrefab;
    [SerializeField] private List<Transform> _spawnPoints = new List<Transform>();
    [SerializeField] private int _maxBoars = 5;
    [SerializeField] private int _maxDeer = 3;
    [SerializeField] private float _spawnInterval = 60f;

    private List<Animal_AI> _activeBoars = new List<Animal_AI>();
    private List<Animal_AI> _activeDeer = new List<Animal_AI>();
    private float _spawnTimer;

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
        // Initial spawn
        SpawnInitialAnimals();
    }

    private void Update()
    {
        // Clean up dead animals
        _activeBoars.RemoveAll(a => a == null || !a.IsAlive);
        _activeDeer.RemoveAll(a => a == null || !a.IsAlive);

        // Spawn new animals over time
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;
            TrySpawnAnimals();
        }
    }

    private void SpawnInitialAnimals()
    {
        for (int i = 0; i < _maxBoars && i < _spawnPoints.Count; i++)
        {
            SpawnBoar(_spawnPoints[i]);
        }
        for (int i = 0; i < _maxDeer && i < _spawnPoints.Count; i++)
        {
            SpawnDeer(_spawnPoints[(i + _maxBoars) % _spawnPoints.Count]);
        }
    }

    private void TrySpawnAnimals()
    {
        if (_activeBoars.Count < _maxBoars)
        {
            SpawnBoar(_spawnPoints[Random.Range(0, _spawnPoints.Count)]);
        }
        if (_activeDeer.Count < _maxDeer)
        {
            SpawnDeer(_spawnPoints[Random.Range(0, _spawnPoints.Count)]);
        }
    }

    private void SpawnBoar(Transform spawnPoint)
    {
        if (_boarPrefab != null && spawnPoint != null)
        {
            var obj = Instantiate(_boarPrefab, spawnPoint.position, spawnPoint.rotation);
            var ai = obj.GetComponent<Animal_AI>();
            if (ai != null) _activeBoars.Add(ai);
        }
    }

    private void SpawnDeer(Transform spawnPoint)
    {
        if (_deerPrefab != null && spawnPoint != null)
        {
            var obj = Instantiate(_deerPrefab, spawnPoint.position, spawnPoint.rotation);
            var ai = obj.GetComponent<Animal_AI>();
            if (ai != null) _activeDeer.Add(ai);
        }
    }

    public void RegisterBoar(Animal_AI boar)
    {
        if (!_activeBoars.Contains(boar))
        {
            _activeBoars.Add(boar);
        }
    }

    public void RegisterDeer(Animal_AI deer)
    {
        if (!_activeDeer.Contains(deer))
        {
            _activeDeer.Add(deer);
        }
    }
}