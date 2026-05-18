using System.Collections.Generic;
using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private DinoEnemy[] enemyPrefabs;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private Vector3 spawnCenter;
        [SerializeField] private Vector2 spawnSize = new(80f, 80f);
        [SerializeField] private float spawnY = 0.75f;
        [SerializeField] private int spawnCount = 24;
        [SerializeField] private float minDistanceFromPlayer = 8f;
        [SerializeField] private Transform player;
        [SerializeField] private float minWanderSpeed = 2.4f;
        [SerializeField] private float maxWanderSpeed = 4.2f;

        private readonly List<DinoEnemy> spawnedEnemies = new();

        private void Start()
        {
            SpawnInitialEnemies();
        }

        private void SpawnInitialEnemies()
        {
            ClearSpawnedEnemies();

            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                Debug.LogWarning("EnemySpawner needs at least one enemy prefab.", this);
                return;
            }

            for (var i = 0; i < spawnCount; i++)
            {
                SpawnOne();
            }
        }

        private void SpawnOne()
        {
            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            if (prefab == null)
            {
                return;
            }

            var position = PickSpawnPosition();
            var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var enemy = Instantiate(prefab, position, rotation, spawnParent);
            enemy.name = $"{prefab.name}_Spawned";

            var wander = enemy.GetComponent<EnemyWanderMovement>();
            if (wander == null)
            {
                wander = enemy.gameObject.AddComponent<EnemyWanderMovement>();
            }

            wander.Configure(spawnCenter, spawnSize, Random.Range(minWanderSpeed, maxWanderSpeed), player);
            spawnedEnemies.Add(enemy);
        }

        private Vector3 PickSpawnPosition()
        {
            for (var i = 0; i < 20; i++)
            {
                var position = RandomPositionInArea();
                if (player == null || Vector3.Distance(position, player.position) >= minDistanceFromPlayer)
                {
                    return position;
                }
            }

            return RandomPositionInArea();
        }

        private Vector3 RandomPositionInArea()
        {
            var halfSize = spawnSize * 0.5f;
            return new Vector3(
                Random.Range(spawnCenter.x - halfSize.x, spawnCenter.x + halfSize.x),
                spawnY,
                Random.Range(spawnCenter.z - halfSize.y, spawnCenter.z + halfSize.y));
        }

        private void ClearSpawnedEnemies()
        {
            for (var i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] != null)
                {
                    Destroy(spawnedEnemies[i].gameObject);
                }
            }

            spawnedEnemies.Clear();
        }
    }
}
