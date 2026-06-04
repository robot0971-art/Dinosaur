using System.Collections;
using System.Collections.Generic;
using DinoGrow.Core.Data;
using DinoGrow.Core.Enemy;
using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;
using DinoGrow.Infrastructure.Data;
using DinoGrow.Infrastructure.Events;
using DinoGrow.Infrastructure.Pooling;
using DinoGrow.Gameplay.Player;
using DinoGrow.Gameplay.Items;
using DinoGrow.Gameplay.VFX;
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
        [SerializeField] private int spawnBatchSize = 4;
        [SerializeField] private LayerMask obstacleLayers = 0;
        [SerializeField] private float obstacleSpawnClearance = 3f;
        [SerializeField, Range(0f, 1f)] private float centerWeightedSpawnChance = 0.65f;
        [SerializeField, Range(0.1f, 1f)] private float centerWeightedSpawnScale = 0.65f;
        [SerializeField, Range(0f, 0.45f)] private float spawnEdgePaddingRatio = 0.12f;
        [Header("Drops")]
        [SerializeField] private GameObject heartDropPrefab;
        [SerializeField] private GameObject heartDropIdleEffectPrefab;
        [SerializeField] private bool enableHeartDropSpawnEffect = true;
        [SerializeField] private GameObject heartPickupEffectPrefab;
        [SerializeField] private AudioClip heartPickupSoundClip;
        [SerializeField, Range(0f, 1f)] private float heartPickupSoundVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float heartDropChance = 0.3f;
        [SerializeField, Min(0)] private int maxSpawnedHeartDrops = 8;
        [SerializeField] private bool enableHeartDropIdleEffects;
        [SerializeField] private float heartDropHeightOffset = 0.35f;
        [SerializeField] private float heartDropKnockbackDistance = 4.5f;
        [SerializeField] private bool logHeartDrops;

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
        private GameStateController gameState;
        private readonly EnemySpawnLevelRule spawnLevelRule = new();
        private readonly EnemySpawnStatsRule spawnStatsRule = new();
        private readonly HeartDropSpawnService heartDropSpawnService = new();
        private readonly EnemySpawnPositionPicker spawnPositionPicker = new();
        private Coroutine spawnRoutine;
        private PlayerDinoController playerController;
        private Vector3 playerStartExclusionCenter;
        private bool hasPlayerStartExclusion;
        private bool mapTransitionInProgress;

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
            EnemyBehaviorResolver enemyBehaviorResolver,
            GameStateController gameState)
        {
            this.dinoDataRepository = dinoDataRepository;
            this.spawnDataRepository = spawnDataRepository;
            this.stageDataRepository = stageDataRepository;
            this.playerProgress = playerProgress;
            this.poolService = poolService;
            this.eventBus = eventBus;
            this.enemyBehaviorResolver = enemyBehaviorResolver;
            this.gameState = gameState;
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
            if (!mapTransitionInProgress)
            {
                RequestSpawnInitialEnemies();
            }
        }

        public void SetMapTransitionInProgress(bool inProgress)
        {
            mapTransitionInProgress = inProgress;
        }

        public IEnumerator RebuildSpawnedEnemiesForMapTransition()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }

            yield return SpawnInitialEnemiesWhenReady();
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

            obstacleLayers = CreateObstacleLayerMask();
        }

        private static LayerMask CreateObstacleLayerMask()
        {
            var mask = 0;
            AddLayerToMask("Obstacle", ref mask);
            AddLayerToMask("MediumDecor", ref mask);
            AddLayerToMask("LargeDecor", ref mask);
            return mask;
        }

        private static void AddLayerToMask(string layerName, ref int mask)
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                mask |= 1 << layer;
            }
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

            yield return SpawnInitialEnemiesRoutine();
            spawnRoutine = null;
        }

        private bool HasNavMeshNearSpawnArea()
        {
            var samplePosition = player != null ? player.position : spawnCenter;
            return NavMesh.SamplePosition(samplePosition, out _, navMeshSampleDistance, NavMesh.AllAreas)
                || NavMesh.SamplePosition(spawnCenter, out _, navMeshSampleDistance, NavMesh.AllAreas);
        }

        private IEnumerator SpawnInitialEnemiesRoutine()
        {
            ClearSpawnedEnemies();

            if (TryGetGameDataSpawnRequests(out var gameDataSpawnRequests))
            {
                var spawnedInBatch = 0;
                foreach (var request in gameDataSpawnRequests)
                {
                    for (var i = 0; i < request.Count; i++)
                    {
                        TrySpawnFromData(request.Prefab, request.DinoData, request.SpawnRecord);
                        spawnedInBatch++;
                        if (ShouldYieldSpawnBatch(spawnedInBatch))
                        {
                            spawnedInBatch = 0;
                            yield return null;
                        }
                    }
                }

                hasPlayerStartExclusion = false;
                yield break;
            }

            try
            {
                if (enemyPrefabs == null || enemyPrefabs.Length == 0)
                {
                    Debug.LogWarning("EnemySpawner needs at least one enemy prefab.", this);
                    yield break;
                }

                var spawnedInBatch = 0;
                for (var i = 0; i < spawnCount; i++)
                {
                    TrySpawnOne();
                    spawnedInBatch++;
                    if (ShouldYieldSpawnBatch(spawnedInBatch))
                    {
                        spawnedInBatch = 0;
                        yield return null;
                    }
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

        private bool TryGetGameDataSpawnRequests(out List<GameDataSpawnRequest> requests)
        {
            requests = new List<GameDataSpawnRequest>();
            if (dinoDataRepository == null || spawnDataRepository == null)
            {
                return false;
            }

            var spawnRecords = spawnDataRepository.GetByStageId(stageId);
            if (spawnRecords.Count == 0)
            {
                return false;
            }

            foreach (var spawnRecord in spawnRecords)
            {
                if (!dinoDataRepository.TryGetById(spawnRecord.dinoId, out var dinoData))
                {
                    Debug.LogWarning($"Dino data was not found for spawn id '{spawnRecord.dinoId}'.", this);
                    continue;
                }

                var prefab = EnemyPrefabResolver.FindByName(enemyPrefabs, dinoData.prefab);
                if (prefab == null)
                {
                    Debug.LogWarning($"Enemy prefab '{dinoData.prefab}' was not assigned to EnemySpawner.", this);
                    continue;
                }

                var count = Mathf.Max(0, spawnRecord.count);
                if (count <= 0)
                {
                    continue;
                }

                requests.Add(new GameDataSpawnRequest(prefab, dinoData, spawnRecord, count));
            }

            return requests.Count > 0;
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
            EnemyScaleApplier.ApplyNormalizedScale(enemy.transform, GetEnemySize(dinoData, level), sizeUnit);

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
                enemyBehaviorResolver,
                gameState);
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

        private void TryDropHeart(Vector3 spawnPosition, Vector3 landingOrigin, Vector3 awayDirection)
        {
            if (!heartDropSpawnService.TryDrop(
                    CreateHeartDropSettings(),
                    CreateHeartDropContext(),
                    spawnPosition,
                    landingOrigin,
                    awayDirection,
                    out var landingPosition))
            {
                if (logHeartDrops)
                {
                    Debug.Log($"Heart drop skipped. Prefab assigned: {heartDropPrefab != null}, chance: {heartDropChance}", this);
                }

                return;
            }

            if (logHeartDrops)
            {
                Debug.Log($"Heart dropped from {spawnPosition} to {landingPosition}.", this);
            }
        }

        private HeartDropSpawnSettings CreateHeartDropSettings()
        {
            return new HeartDropSpawnSettings(
                heartDropPrefab,
                heartDropIdleEffectPrefab,
                enableHeartDropSpawnEffect,
                heartPickupEffectPrefab,
                heartPickupSoundClip,
                heartPickupSoundVolume,
                heartDropChance,
                maxSpawnedHeartDrops,
                enableHeartDropIdleEffects,
                heartDropHeightOffset,
                heartDropKnockbackDistance);
        }

        private HeartDropSpawnContext CreateHeartDropContext()
        {
            return new HeartDropSpawnContext(
                spawnParent,
                spawnedHeartDrops,
                poolService,
                () => Random.value,
                () => Random.insideUnitSphere,
                SnapToGround,
                ClampToSpawnArea);
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

            var awayFromPlayer = player != null
                ? enemy.transform.position - player.position
                : enemy.transform.forward;
            TryDropHeart(enemy.GetMouthEffectPosition(), enemy.transform.position, awayFromPlayer);
        }

        private void OnPlayerGrowthChanged(Core.Growth.GrowthResult result)
        {
            RefreshSpawnedEnemyLevelTextColors();

            if (!result.LeveledUp)
            {
                return;
            }

            if (mapTransitionInProgress)
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

        private int GetSpawnLevel(SpawnDataRecord spawnRecord)
        {
            var context = new EnemySpawnLevelContext(
                scaleEnemyLevelWithPlayer,
                playerProgress != null,
                playerProgress != null ? playerProgress.Level : 1,
                playerProgress != null ? playerProgress.MaxLevel : PlayerProgress.DefaultMaxLevel,
                edibleLevelOffset,
                threatLevelOffset,
                threatSpawnChance);
            return spawnLevelRule.GetSpawnLevel(
                spawnRecord,
                context,
                () => Random.value,
                (minLevel, maxLevel) => Random.Range(minLevel, maxLevel + 1));
        }

        private float GetEnemySize(DinoDataRecord dinoData, int level)
        {
            return spawnStatsRule.GetEnemySize(dinoData, level);
        }

        private float GetMoveSpeed(DinoDataRecord dinoData, SpawnDataRecord spawnRecord)
        {
            return spawnStatsRule.GetMoveSpeed(
                dinoData,
                spawnRecord,
                minWanderSpeed,
                maxWanderSpeed,
                Random.Range);
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
            ConfigureSpawnPositionPicker();
            return spawnPositionPicker.TryPick(spawnedEnemies, out spawnPosition);
        }

        private Vector3 ClampToSpawnArea(Vector3 position)
        {
            ConfigureSpawnPositionPicker();
            return spawnPositionPicker.ClampToSpawnArea(position);
        }

        private Vector3 SnapToGround(Vector3 position)
        {
            ConfigureSpawnPositionPicker();
            return spawnPositionPicker.SnapToGround(position);
        }

        private void ConfigureSpawnPositionPicker()
        {
            spawnPositionPicker.Configure(new EnemySpawnPositionSettings(
                spawnCenter,
                spawnSize,
                spawnY,
                groundLayers,
                groundRaycastHeight,
                groundRaycastDistance,
                groundOffset,
                maxSpawnAttemptsPerEnemy,
                minDistanceFromPlayer,
                avoidPlayerStartArea,
                playerStartExclusionRadius,
                playerStartExclusionCenter,
                hasPlayerStartExclusion,
                minDistanceBetweenEnemies,
                player,
                navMeshSampleDistance,
                navMeshVerticalSampleDistance,
                obstacleLayers,
                obstacleSpawnClearance,
                centerWeightedSpawnChance,
                centerWeightedSpawnScale,
                spawnEdgePaddingRatio));
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
            heartDropSpawnService.ClearDrops(spawnedHeartDrops, poolService);
            spawnedHeartDrops.Clear();
        }

        private bool ShouldYieldSpawnBatch(int spawnedInBatch)
        {
            return Application.isPlaying && spawnedInBatch >= Mathf.Max(1, spawnBatchSize);
        }

        private readonly struct GameDataSpawnRequest
        {
            public GameDataSpawnRequest(
                DinoEnemy prefab,
                DinoDataRecord dinoData,
                SpawnDataRecord spawnRecord,
                int count)
            {
                Prefab = prefab;
                DinoData = dinoData;
                SpawnRecord = spawnRecord;
                Count = count;
            }

            public DinoEnemy Prefab { get; }
            public DinoDataRecord DinoData { get; }
            public SpawnDataRecord SpawnRecord { get; }
            public int Count { get; }
        }
    }
}
