using System.Collections.Generic;
using DinoGrow.Core.Data;
using DinoGrow.Infrastructure.Data;
using DinoGrow.Infrastructure.Events;
using DinoGrow.Infrastructure.Pooling;
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
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float groundRaycastHeight = 50f;
        [SerializeField] private float groundRaycastDistance = 120f;
        [SerializeField] private float groundOffset = 0f;
        [SerializeField] private int spawnCount = 24;
        [SerializeField] private float minDistanceFromPlayer = 8f;
        [SerializeField] private Transform player;
        [SerializeField] private float minWanderSpeed = 2.4f;
        [SerializeField] private float maxWanderSpeed = 4.2f;
        [SerializeField] private float sizeUnit = 3f;
        [SerializeField] private bool advanceStageOnLevelUpForTest = true;

        private readonly List<DinoEnemy> spawnedEnemies = new();
        private DinoDataRepository dinoDataRepository;
        private SpawnDataRepository spawnDataRepository;
        private StageDataRepository stageDataRepository;
        private IObjectPoolService poolService;
        private GameEventBus eventBus;

        [Inject]
        public void Construct(
            DinoDataRepository dinoDataRepository,
            SpawnDataRepository spawnDataRepository,
            StageDataRepository stageDataRepository,
            IObjectPoolService poolService,
            GameEventBus eventBus)
        {
            this.dinoDataRepository = dinoDataRepository;
            this.spawnDataRepository = spawnDataRepository;
            this.stageDataRepository = stageDataRepository;
            this.poolService = poolService;
            this.eventBus = eventBus;
        }

        private void Start()
        {
            if (eventBus != null)
            {
                eventBus.PlayerGrowthChanged += OnPlayerGrowthChanged;
            }

            ApplyStageData();
            SpawnInitialEnemies();
        }

        private void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.PlayerGrowthChanged -= OnPlayerGrowthChanged;
            }
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
            var enemy = SpawnEnemy(prefab, position, rotation);
            enemy.name = $"{prefab.name}_Spawned";
            enemy.SetDespawnHandler(DespawnEnemy);

            var wander = enemy.GetComponent<EnemyWanderMovement>();
            if (wander == null)
            {
                wander = enemy.gameObject.AddComponent<EnemyWanderMovement>();
            }

            wander.Configure(spawnCenter, spawnSize, Random.Range(minWanderSpeed, maxWanderSpeed), player);
            spawnedEnemies.Add(enemy);
            eventBus?.PublishEnemySpawned(enemy.Level);
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
            var enemy = SpawnEnemy(prefab, position, rotation);
            var level = GetRandomLevel(spawnRecord);
            enemy.name = $"{prefab.name}_Lv{level}";
            enemy.SetLevel(level);
            enemy.SetDespawnHandler(DespawnEnemy);
            ApplyNormalizedScale(enemy.transform, GetEnemySize(dinoData, level));

            var wander = enemy.GetComponent<EnemyWanderMovement>();
            if (wander == null)
            {
                wander = enemy.gameObject.AddComponent<EnemyWanderMovement>();
            }

            wander.Configure(spawnCenter, spawnSize, GetMoveSpeed(dinoData, spawnRecord), player);
            spawnedEnemies.Add(enemy);
            eventBus?.PublishEnemySpawned(enemy.Level);
        }

        private DinoEnemy SpawnEnemy(DinoEnemy prefab, Vector3 position, Quaternion rotation)
        {
            if (poolService != null)
            {
                return poolService.Spawn(prefab, position, rotation, spawnParent);
            }

            return Instantiate(prefab, position, rotation, spawnParent);
        }

        private void DespawnEnemy(DinoEnemy enemy)
        {
            if (enemy == null)
            {
                return;
            }

            spawnedEnemies.Remove(enemy);
            eventBus?.PublishEnemyDespawned(enemy.Level);

            if (poolService != null)
            {
                poolService.Despawn(enemy);
                return;
            }

            Destroy(enemy.gameObject);
        }

        private void OnPlayerGrowthChanged(Core.Growth.GrowthResult result)
        {
            RefreshSpawnedEnemyLevelTextColors();

            if (!advanceStageOnLevelUpForTest || !result.LeveledUp)
            {
                return;
            }

            AdvanceToNextStageForTest();
        }

        private void RefreshSpawnedEnemyLevelTextColors()
        {
            foreach (var enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    enemy.RefreshLevelTextColor();
                }
            }
        }

        private void AdvanceToNextStageForTest()
        {
            var nextStageId = stageId + 1;
            if (stageDataRepository == null || !stageDataRepository.TryGetByStageId(nextStageId, out _))
            {
                Debug.Log($"Next stage data was not found for stage {nextStageId}.", this);
                return;
            }

            stageId = nextStageId;
            ApplyStageData();
            SpawnInitialEnemies();
            Debug.Log($"Advanced to stage {stageId} for level-up test.", this);
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

        private static float GetEnemySize(DinoDataRecord dinoData, int level)
        {
            var baseSize = dinoData.size > 0f ? dinoData.size : 1f;
            var baseLevel = Mathf.Max(1, dinoData.level);
            var levelBonus = Mathf.Max(0, level - baseLevel) * 0.08f;
            return baseSize * (1f + levelBonus);
        }

        private void ApplyNormalizedScale(Transform enemyTransform, float targetSize)
        {
            var renderers = enemyTransform.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                enemyTransform.localScale = Vector3.one * targetSize;
                return;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var currentSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (currentSize <= 0.001f)
            {
                enemyTransform.localScale = Vector3.one * targetSize;
                return;
            }

            var targetWorldSize = Mathf.Max(0.1f, targetSize * sizeUnit);
            var scaleMultiplier = targetWorldSize / currentSize;
            enemyTransform.localScale *= scaleMultiplier;
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
            var position = new Vector3(
                Random.Range(spawnCenter.x - halfSize.x, spawnCenter.x + halfSize.x),
                spawnY,
                Random.Range(spawnCenter.z - halfSize.y, spawnCenter.z + halfSize.y));

            return SnapToGround(position);
        }

        private Vector3 SnapToGround(Vector3 position)
        {
            var origin = new Vector3(position.x, position.y + groundRaycastHeight, position.z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, groundRaycastDistance, groundLayers, QueryTriggerInteraction.Ignore))
            {
                position.y = hit.point.y + groundOffset;
            }

            return position;
        }

        private void ClearSpawnedEnemies()
        {
            for (var i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] != null && !spawnedEnemies[i].IsDying)
                {
                    DespawnEnemy(spawnedEnemies[i]);
                }
            }

            spawnedEnemies.RemoveAll(enemy => enemy == null || !enemy.IsDying);
        }
    }
}
