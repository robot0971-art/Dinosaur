using System.Collections.Generic;
using DinoGrow.Core.Data;
using DinoGrow.Infrastructure.Data;
using UnityEngine;
using VContainer;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private int stageId = 1;
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
        private DinoDataRepository dinoDataRepository;
        private SpawnDataRepository spawnDataRepository;
        private StageDataRepository stageDataRepository;

        [Inject]
        public void Construct(
            DinoDataRepository dinoDataRepository,
            SpawnDataRepository spawnDataRepository,
            StageDataRepository stageDataRepository)
        {
            this.dinoDataRepository = dinoDataRepository;
            this.spawnDataRepository = spawnDataRepository;
            this.stageDataRepository = stageDataRepository;
        }

        private void Start()
        {
            ApplyStageData();
            SpawnInitialEnemies();
        }

        private void SpawnInitialEnemies()
        {
            ClearSpawnedEnemies();

            if (TrySpawnFromGameData())
            {
                return;
            }

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

        private bool TrySpawnFromGameData()
        {
            if (dinoDataRepository == null || spawnDataRepository == null)
            {
                return false;
            }

            var spawnRecords = spawnDataRepository.GetByStageId(stageId);
            if (spawnRecords.Count == 0)
            {
                return false;
            }

            var spawnedAny = false;
            foreach (var spawnRecord in spawnRecords)
            {
                if (!dinoDataRepository.TryGetById(spawnRecord.dinoId, out var dinoData))
                {
                    Debug.LogWarning($"Dino data was not found for spawn id '{spawnRecord.dinoId}'.", this);
                    continue;
                }

                var prefab = FindPrefabByName(dinoData.prefab);
                if (prefab == null)
                {
                    Debug.LogWarning($"Enemy prefab '{dinoData.prefab}' was not assigned to EnemySpawner.", this);
                    continue;
                }

                var count = Mathf.Max(0, spawnRecord.count);
                for (var i = 0; i < count; i++)
                {
                    SpawnFromData(prefab, dinoData, spawnRecord);
                    spawnedAny = true;
                }
            }

            return spawnedAny;
        }

        private void SpawnFromData(DinoEnemy prefab, DinoDataRecord dinoData, SpawnDataRecord spawnRecord)
        {
            var position = PickSpawnPosition();
            var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var enemy = Instantiate(prefab, position, rotation, spawnParent);
            var level = GetRandomLevel(spawnRecord);
            enemy.name = $"{prefab.name}_Lv{level}";
            enemy.SetLevel(level);
            enemy.transform.localScale = Vector3.one * GetEnemyScale(dinoData, level);

            var wander = enemy.GetComponent<EnemyWanderMovement>();
            if (wander == null)
            {
                wander = enemy.gameObject.AddComponent<EnemyWanderMovement>();
            }

            wander.Configure(spawnCenter, spawnSize, GetMoveSpeed(dinoData, spawnRecord), player);
            spawnedEnemies.Add(enemy);
        }

        private DinoEnemy FindPrefabByName(string prefabName)
        {
            if (string.IsNullOrWhiteSpace(prefabName) || enemyPrefabs == null)
            {
                return null;
            }

            foreach (var prefab in enemyPrefabs)
            {
                if (prefab != null && prefab.name == prefabName)
                {
                    return prefab;
                }
            }

            return null;
        }

        private static int GetRandomLevel(SpawnDataRecord spawnRecord)
        {
            var minLevel = Mathf.Max(1, spawnRecord.minLevel);
            var maxLevel = Mathf.Max(minLevel, spawnRecord.maxLevel);
            return Random.Range(minLevel, maxLevel + 1);
        }

        private static float GetEnemyScale(DinoDataRecord dinoData, int level)
        {
            var baseSize = dinoData.size > 0f ? dinoData.size : 1f;
            var baseLevel = Mathf.Max(1, dinoData.level);
            var levelBonus = Mathf.Max(0, level - baseLevel) * 0.08f;
            return baseSize * (1f + levelBonus);
        }

        private static float GetMoveSpeed(DinoDataRecord dinoData, SpawnDataRecord spawnRecord)
        {
            if (spawnRecord.maxWanderSpeed > 0f)
            {
                var minSpeed = Mathf.Max(0f, spawnRecord.minWanderSpeed);
                var maxSpeed = Mathf.Max(minSpeed, spawnRecord.maxWanderSpeed);
                return Random.Range(minSpeed, maxSpeed);
            }

            return Mathf.Max(0f, dinoData.speed);
        }

        private void ApplyStageData()
        {
            if (stageDataRepository == null || !stageDataRepository.TryGetByStageId(stageId, out var stageData))
            {
                return;
            }

            spawnCenter = new Vector3(stageData.spawnCenterX, 0f, stageData.spawnCenterZ);
            spawnSize = new Vector2(stageData.spawnSizeX, stageData.spawnSizeZ);
            spawnY = stageData.spawnY;
            minDistanceFromPlayer = stageData.minDistanceFromPlayer;
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
