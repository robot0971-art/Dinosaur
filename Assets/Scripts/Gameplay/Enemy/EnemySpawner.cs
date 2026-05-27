using System.Collections;
using System.Collections.Generic;
using DinoGrow.Core.Data;
using DinoGrow.Core.Enemy;
using DinoGrow.Core.Growth;
using DinoGrow.Infrastructure.Data;
using DinoGrow.Infrastructure.Events;
using DinoGrow.Infrastructure.Pooling;
using DinoGrow.Gameplay.Player;
using UnityEngine;
using UnityEngine.AI;
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
        [SerializeField] private bool avoidPlayerStartArea = true;
        [SerializeField] private float playerStartExclusionRadius = 32f;
        [SerializeField] private float minDistanceBetweenEnemies = 9f;
        [SerializeField] private Transform player;
        [SerializeField] private float minWanderSpeed = 4.2f;
        [SerializeField] private float maxWanderSpeed = 6.8f;
        [SerializeField] private float sizeUnit = 3f;
        [SerializeField] private bool advanceStageOnLevelUpForTest = true;
        [SerializeField] private bool respawnCurrentStageOnLevelUp = true;
        [SerializeField] private bool scaleEnemyLevelWithPlayer = true;
        [SerializeField] private int edibleLevelOffset = 1;
        [SerializeField] private int threatLevelOffset = 2;
        [SerializeField, Range(0f, 1f)] private float threatSpawnChance = 0.25f;
        [SerializeField] private float navMeshWaitTimeout = 3f;
        [SerializeField] private float navMeshSampleDistance = 8f;
        [SerializeField] private float navMeshVerticalSampleDistance = 80f;
        [SerializeField] private int maxSpawnAttemptsPerEnemy = 80;
        [SerializeField] private LayerMask obstacleLayers = 0;
        [SerializeField] private float obstacleSpawnClearance = 3f;
        [Header("Drops")]
        [SerializeField] private GameObject heartDropPrefab;
        [SerializeField, Range(0f, 1f)] private float heartDropChance = 0.2f;
        [SerializeField] private float heartDropHeightOffset = 0.35f;

        private readonly List<DinoEnemy> spawnedEnemies = new();
        private readonly List<Transform> spawnedHeartDrops = new();
        private readonly HashSet<DinoEnemy> heartDroppedEnemies = new();
        private DinoDataRepository dinoDataRepository;
        private SpawnDataRepository spawnDataRepository;
        private StageDataRepository stageDataRepository;
        private PlayerProgress playerProgress;
        private IObjectPoolService poolService;
        private GameEventBus eventBus;
        private EnemyBehaviorResolver enemyBehaviorResolver;
        private Coroutine spawnRoutine;
        private PlayerDinoController playerController;
        private Vector3 playerStartExclusionCenter;
        private bool hasPlayerStartExclusion;

        public void ConfigureSpawnArea(Vector3 center, Vector2 size, bool respawn)
        {
            spawnCenter = center;
            spawnSize = new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
            ApplyPlayerMovementBounds();

            if (respawn && isActiveAndEnabled)
            {
                RequestSpawnInitialEnemies();
            }
        }

        [Inject]
        public void Construct(
            DinoDataRepository dinoDataRepository,
            SpawnDataRepository spawnDataRepository,
            StageDataRepository stageDataRepository,
            PlayerProgress playerProgress,
            IObjectPoolService poolService,
            GameEventBus eventBus,
            EnemyBehaviorResolver enemyBehaviorResolver)
        {
            this.dinoDataRepository = dinoDataRepository;
            this.spawnDataRepository = spawnDataRepository;
            this.stageDataRepository = stageDataRepository;
            this.playerProgress = playerProgress;
            this.poolService = poolService;
            this.eventBus = eventBus;
            this.enemyBehaviorResolver = enemyBehaviorResolver;
        }

        private void Start()
        {
            if (eventBus != null)
            {
                eventBus.PlayerGrowthChanged += OnPlayerGrowthChanged;
            }

            UseGroundLayerIfAvailable();
            UseObstacleLayerIfAvailable();
            ApplyStageData();
            RequestSpawnInitialEnemies();
        }

        private void UseGroundLayerIfAvailable()
        {
            var groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0 || groundLayers.value != ~0)
            {
                return;
            }

            groundLayers = 1 << groundLayer;
        }

        private void UseObstacleLayerIfAvailable()
        {
            if (obstacleLayers.value != 0)
            {
                return;
            }

            var obstacleLayer = LayerMask.NameToLayer("Obstacle");
            obstacleLayers = obstacleLayer >= 0
                ? 1 << obstacleLayer
                : 0;
        }

        private void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.PlayerGrowthChanged -= OnPlayerGrowthChanged;
            }
        }

        private void RequestSpawnInitialEnemies()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
            }

            spawnRoutine = StartCoroutine(SpawnInitialEnemiesWhenReady());
        }

        private IEnumerator SpawnInitialEnemiesWhenReady()
        {
            var startTime = Time.time;
            while (!HasNavMeshNearSpawnArea() && Time.time - startTime < navMeshWaitTimeout)
            {
                yield return null;
            }

            spawnRoutine = null;
            SpawnInitialEnemies();
        }

        private bool HasNavMeshNearSpawnArea()
        {
            var samplePosition = player != null ? player.position : spawnCenter;
            return NavMesh.SamplePosition(samplePosition, out _, navMeshSampleDistance, NavMesh.AllAreas)
                || NavMesh.SamplePosition(spawnCenter, out _, navMeshSampleDistance, NavMesh.AllAreas);
        }

        private void SpawnInitialEnemies()
        {
            try
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
                    TrySpawnOne();
                }
            }
            finally
            {
                hasPlayerStartExclusion = false;
            }
        }

        private bool TrySpawnOne()
        {
            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            if (prefab == null)
            {
                return false;
            }

            if (!TryPickSpawnPosition(out var position))
            {
                Debug.LogWarning("EnemySpawner could not find a valid spawn position.", this);
                return false;
            }

            var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var enemy = SpawnEnemy(prefab, position, rotation);
            enemy.name = $"{prefab.name}_Spawned";
            enemy.SetEatenHandler(DropHeartForEnemy);
            enemy.SetDespawnHandler(DespawnEnemy);

            var wander = enemy.GetComponent<EnemyWanderMovement>();
            if (wander == null)
            {
                wander = enemy.gameObject.AddComponent<EnemyWanderMovement>();
            }

            wander.Configure(
                spawnCenter,
                spawnSize,
                Random.Range(minWanderSpeed, maxWanderSpeed),
                player,
                enemyBehaviorResolver);
            spawnedEnemies.Add(enemy);
            eventBus?.PublishEnemySpawned(enemy.Level);
            return true;
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
                    spawnedAny |= TrySpawnFromData(prefab, dinoData, spawnRecord);
                }
            }

            return spawnedAny;
        }

        private bool TrySpawnFromData(DinoEnemy prefab, DinoDataRecord dinoData, SpawnDataRecord spawnRecord)
        {
            if (!TryPickSpawnPosition(out var position))
            {
                Debug.LogWarning($"EnemySpawner could not find a valid spawn position for '{prefab.name}'.", this);
                return false;
            }

            var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var enemy = SpawnEnemy(prefab, position, rotation);
            var level = GetSpawnLevel(spawnRecord);
            enemy.name = $"{prefab.name}_Lv{level}";
            enemy.SetLevel(level);
            enemy.SetEatenHandler(DropHeartForEnemy);
            enemy.SetDespawnHandler(DespawnEnemy);
            ApplyNormalizedScale(enemy.transform, GetEnemySize(dinoData, level));

            var wander = enemy.GetComponent<EnemyWanderMovement>();
            if (wander == null)
            {
                wander = enemy.gameObject.AddComponent<EnemyWanderMovement>();
            }

            wander.Configure(
                spawnCenter,
                spawnSize,
                GetMoveSpeed(dinoData, spawnRecord),
                player,
                enemyBehaviorResolver);
            spawnedEnemies.Add(enemy);
            eventBus?.PublishEnemySpawned(enemy.Level);
            return true;
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
            DespawnEnemy(enemy, true);
        }

        private void DespawnEnemy(DinoEnemy enemy, bool dropHeart)
        {
            if (enemy == null)
            {
                return;
            }

            if (dropHeart)
            {
                DropHeartForEnemy(enemy);
            }

            spawnedEnemies.Remove(enemy);
            heartDroppedEnemies.Remove(enemy);
            eventBus?.PublishEnemyDespawned(enemy.Level);

            if (poolService != null)
            {
                poolService.Despawn(enemy);
                return;
            }

            Destroy(enemy.gameObject);
        }

        private void TryDropHeart(Vector3 position)
        {
            if (heartDropPrefab == null || heartDropChance <= 0f || Random.value > heartDropChance)
            {
                Debug.Log($"Heart drop skipped. Prefab assigned: {heartDropPrefab != null}, chance: {heartDropChance}", this);
                return;
            }

            var dropPosition = SnapToGround(position);
            dropPosition.y += heartDropHeightOffset;
            SpawnHeartDrop(dropPosition);
            Debug.Log($"Heart dropped at {dropPosition}.", this);
        }

        private void SpawnHeartDrop(Vector3 position)
        {
            Transform heartDrop;
            if (poolService != null)
            {
                heartDrop = poolService.Spawn(heartDropPrefab.transform, position, Quaternion.identity, spawnParent);
            }
            else
            {
                heartDrop = Instantiate(heartDropPrefab, position, Quaternion.identity, spawnParent).transform;
            }

            if (heartDrop != null && !spawnedHeartDrops.Contains(heartDrop))
            {
                spawnedHeartDrops.Add(heartDrop);
            }
        }

        public void ConfigurePlayerStartExclusion(Vector3 center, bool enabled)
        {
            playerStartExclusionCenter = center;
            hasPlayerStartExclusion = enabled;
        }

        private void DropHeartForEnemy(DinoEnemy enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (!heartDroppedEnemies.Add(enemy))
            {
                return;
            }

            TryDropHeart(enemy.transform.position);
        }

        private void OnPlayerGrowthChanged(Core.Growth.GrowthResult result)
        {
            RefreshSpawnedEnemyLevelTextColors();

            if (!result.LeveledUp)
            {
                return;
            }

            var advancedStage = advanceStageOnLevelUpForTest && AdvanceToNextStageForTest();
            if (!advancedStage && respawnCurrentStageOnLevelUp)
            {
                RequestSpawnInitialEnemies();
            }
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

        private bool AdvanceToNextStageForTest()
        {
            var nextStageId = stageId + 1;
            if (stageDataRepository == null || !stageDataRepository.TryGetByStageId(nextStageId, out _))
            {
                return false;
            }

            stageId = nextStageId;
            ApplyStageData();
            RequestSpawnInitialEnemies();
            return true;
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

        private int GetSpawnLevel(SpawnDataRecord spawnRecord)
        {
            var minLevel = Mathf.Max(1, spawnRecord.minLevel);
            var maxLevel = Mathf.Max(minLevel, spawnRecord.maxLevel);

            if (!scaleEnemyLevelWithPlayer || playerProgress == null)
            {
                return Random.Range(minLevel, maxLevel + 1);
            }

            var playerLevel = playerProgress.Level;
            var maxPossibleLevel = Mathf.Max(1, playerProgress.MaxLevel);
            var shouldSpawnThreat = playerLevel < maxPossibleLevel && Random.value < threatSpawnChance;
            if (shouldSpawnThreat)
            {
                minLevel = Mathf.Max(minLevel, playerLevel + 1);
                maxLevel = Mathf.Max(minLevel, playerLevel + threatLevelOffset);
            }
            else
            {
                minLevel = Mathf.Max(1, playerLevel - edibleLevelOffset);
                maxLevel = Mathf.Max(minLevel, playerLevel);
            }

            minLevel = Mathf.Clamp(minLevel, 1, maxPossibleLevel);
            maxLevel = Mathf.Clamp(maxLevel, minLevel, maxPossibleLevel);
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

        private float GetMoveSpeed(DinoDataRecord dinoData, SpawnDataRecord spawnRecord)
        {
            if (spawnRecord.maxWanderSpeed > 0f)
            {
                var minSpeed = Mathf.Max(minWanderSpeed, spawnRecord.minWanderSpeed);
                var maxSpeed = Mathf.Max(minSpeed, spawnRecord.maxWanderSpeed, maxWanderSpeed);
                return Random.Range(minSpeed, maxSpeed);
            }

            return Mathf.Clamp(dinoData.speed, minWanderSpeed, maxWanderSpeed);
        }

        private void ApplyStageData()
        {
            if (stageDataRepository == null || !stageDataRepository.TryGetByStageId(stageId, out var stageData))
            {
                ApplyPlayerMovementBounds();
                return;
            }

            spawnCenter = new Vector3(stageData.spawnCenterX, 0f, stageData.spawnCenterZ);
            spawnSize = new Vector2(stageData.spawnSizeX, stageData.spawnSizeZ);
            spawnY = stageData.spawnY;
            minDistanceFromPlayer = stageData.minDistanceFromPlayer;
            ApplyPlayerMovementBounds();
        }

        private void ApplyPlayerMovementBounds()
        {
            if (player == null)
            {
                return;
            }

            if (playerController == null)
            {
                playerController = player.GetComponent<PlayerDinoController>();
            }

            playerController?.SetMovementBounds(spawnCenter, spawnSize);
        }

        private bool TryPickSpawnPosition(out Vector3 spawnPosition)
        {
            var attempts = Mathf.Max(1, maxSpawnAttemptsPerEnemy);
            for (var i = 0; i < attempts; i++)
            {
                var position = RandomPositionInArea();
                if (!TrySnapToNavMesh(position, out position))
                {
                    continue;
                }

                if (!IsValidSpawnPosition(position))
                {
                    continue;
                }

                spawnPosition = position;
                return true;
            }

            for (var i = 0; i < attempts; i++)
            {
                var position = RandomPositionInArea();
                if (!TryGetBoundedGroundPosition(position, out position))
                {
                    continue;
                }

                if (IsTooCloseToPlayerArea(position))
                {
                    continue;
                }

                spawnPosition = position;
                return true;
            }

            spawnPosition = Vector3.zero;
            return false;
        }

        private bool TrySnapToNavMesh(Vector3 position, out Vector3 navPosition)
        {
            position = SnapToGround(position);
            var navSearchRadius = Mathf.Max(0.1f, navMeshSampleDistance);
            var verticalTolerance = Mathf.Max(0.1f, navMeshVerticalSampleDistance);
            if (NavMesh.SamplePosition(position, out var hit, navSearchRadius, NavMesh.AllAreas)
                && Mathf.Abs(hit.position.y - position.y) <= verticalTolerance
                && IsInsideSpawnArea(hit.position))
            {
                navPosition = hit.position;
                return true;
            }

            navPosition = position;
            return IsInsideSpawnArea(navPosition) && TryGetGroundY(navPosition, out _);
        }

        private bool TryGetBoundedGroundPosition(Vector3 position, out Vector3 groundPosition)
        {
            position = ClampToSpawnArea(position);
            if (TryGetGroundY(position, out var groundY))
            {
                position.y = groundY;
                groundPosition = ClampToSpawnArea(position);
                return true;
            }

            var navSearchRadius = Mathf.Max(0.1f, navMeshSampleDistance);
            if (NavMesh.SamplePosition(position, out var hit, navSearchRadius, NavMesh.AllAreas))
            {
                groundPosition = ClampToSpawnArea(hit.position);
                return true;
            }

            groundPosition = Vector3.zero;
            return false;
        }

        private bool IsValidSpawnPosition(Vector3 position)
        {
            if (!IsInsideSpawnArea(position))
            {
                return false;
            }

            if (IsNearObstacle(position))
            {
                return false;
            }

            if (IsTooCloseToPlayerArea(position))
            {
                return false;
            }

            var minEnemyDistanceSqr = minDistanceBetweenEnemies * minDistanceBetweenEnemies;
            foreach (var enemy in spawnedEnemies)
            {
                if (enemy == null || enemy.IsDying)
                {
                    continue;
                }

                var offset = position - enemy.transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude < minEnemyDistanceSqr)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsTooCloseToPlayerArea(Vector3 position)
        {
            var minDistance = Mathf.Max(0f, minDistanceFromPlayer);
            var minDistanceSqr = minDistance * minDistance;
            if (player != null)
            {
                var offset = position - player.position;
                offset.y = 0f;
                if (offset.sqrMagnitude < minDistanceSqr)
                {
                    return true;
                }
            }

            if (avoidPlayerStartArea && hasPlayerStartExclusion)
            {
                minDistance = Mathf.Max(minDistance, playerStartExclusionRadius);
                minDistanceSqr = minDistance * minDistance;
                var offset = position - playerStartExclusionCenter;
                offset.y = 0f;
                if (offset.sqrMagnitude < minDistanceSqr)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsInsideSpawnArea(Vector3 position)
        {
            var halfSize = spawnSize * 0.5f;
            return position.x >= spawnCenter.x - halfSize.x
                && position.x <= spawnCenter.x + halfSize.x
                && position.z >= spawnCenter.z - halfSize.y
                && position.z <= spawnCenter.z + halfSize.y;
        }

        private Vector3 ClampToSpawnArea(Vector3 position)
        {
            var halfSize = spawnSize * 0.5f;
            position.x = Mathf.Clamp(position.x, spawnCenter.x - halfSize.x, spawnCenter.x + halfSize.x);
            position.z = Mathf.Clamp(position.z, spawnCenter.z - halfSize.y, spawnCenter.z + halfSize.y);
            return position;
        }

        private bool IsNearObstacle(Vector3 position)
        {
            if (obstacleLayers.value == 0)
            {
                return false;
            }

            var radius = Mathf.Max(0.1f, obstacleSpawnClearance);
            return Physics.CheckSphere(
                position + Vector3.up * radius,
                radius,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);
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

        private bool TryGetGroundY(Vector3 position, out float groundY)
        {
            groundY = position.y;
            var origin = new Vector3(position.x, position.y + groundRaycastHeight, position.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, groundRaycastDistance, groundLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                return false;
            }

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
            {
                if (!IsGroundCollider(hit.collider))
                {
                    continue;
                }

                groundY = hit.point.y + groundOffset;
                return true;
            }

            return false;
        }

        private Vector3 SnapToGround(Vector3 position)
        {
            var origin = new Vector3(position.x, position.y + groundRaycastHeight, position.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, groundRaycastDistance, groundLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                return position;
            }

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
            {
                if (!IsGroundCollider(hit.collider))
                {
                    continue;
                }

                position.y = hit.point.y + groundOffset;
                return position;
            }

            return position;
        }

        private static bool IsGroundCollider(Collider targetCollider)
        {
            if (targetCollider == null)
            {
                return false;
            }

            var target = targetCollider.transform;
            while (target != null)
            {
                if (IsNonGroundSurfaceName(target.name))
                {
                    return false;
                }

                target = target.parent;
            }

            return true;
        }

        private static bool IsNonGroundSurfaceName(string targetName)
        {
            return targetName == "Water"
                || targetName == "MapBoundary"
                || targetName.StartsWith("Tree_", System.StringComparison.Ordinal)
                || targetName.StartsWith("Rock_", System.StringComparison.Ordinal);
        }

        private void ClearSpawnedEnemies()
        {
            ClearSpawnedHeartDrops();

            for (var i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] != null && !spawnedEnemies[i].IsDying)
                {
                    DespawnEnemy(spawnedEnemies[i], false);
                }
            }

            spawnedEnemies.RemoveAll(enemy => enemy == null || !enemy.IsDying);
            heartDroppedEnemies.Clear();
        }

        private void ClearSpawnedHeartDrops()
        {
            for (var i = spawnedHeartDrops.Count - 1; i >= 0; i--)
            {
                var heartDrop = spawnedHeartDrops[i];
                if (heartDrop == null)
                {
                    continue;
                }

                if (poolService != null)
                {
                    poolService.Despawn(heartDrop);
                    continue;
                }

                Destroy(heartDrop.gameObject);
            }

            spawnedHeartDrops.Clear();
        }
    }
}
