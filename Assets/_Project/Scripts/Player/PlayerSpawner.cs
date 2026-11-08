using System;
using System.Collections;
using UnityEngine;
using Lake.Core;
using Lake.Health;

namespace Lake.Player
{
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] GameObject playerPrefab;
        [SerializeField] SpawnPoint[] spawnPoints;
        [SerializeField] float respawnDelay = 2f;

        GameObject _current;

        public event Action<GameObject> OnPlayerSpawned;
        public event Action OnPlayerDied;

        void Start() => Spawn();

        public void Spawn()
        {
            Vector3 pos = new(0f, 1.1f, -20f);
            Quaternion rot = Quaternion.identity;
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                var sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
                pos = sp.transform.position;
                rot = sp.transform.rotation;
            }

            _current = Instantiate(playerPrefab, pos, rot);
            var health = _current.GetComponent<HealthComponent>();
            if (health != null)
                health.OnDeath += OnPlayerDeath;

            OnPlayerSpawned?.Invoke(_current);
        }

        void OnPlayerDeath(DamageInfo info)
        {
            OnPlayerDied?.Invoke();
            StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            if (_current != null) Destroy(_current);
            Spawn();
        }
    }
}