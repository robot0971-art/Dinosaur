using System.Collections.Generic;
using DinoGrow.Gameplay.Player;
using UnityEngine;
using UnityEngine.AI;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class EnemySpawnPositionPicker
    {
        private static readonly Collider[] ObstacleOverlapHits = new Collider[32];

        private EnemySpawnAreaRule spawnAreaRule;
        private EnemySpawnPositionSettings settings;

        public void Configure(EnemySpawnPositionSettings settings)
        {
            this.settings = settings;
            if (spawnAreaRule == null)
            {
                spawnAreaRule = new EnemySpawnAreaRule(settings.SpawnCenter, settings.SpawnSize, settings.SpawnEdgePaddingRatio);
                return;
            }

            spawnAreaRule.Configure(settings.SpawnCenter, settings.SpawnSize, settings.SpawnEdgePaddingRatio);
        }

        public bool TryPick(IReadOnlyList<DinoEnemy> spawnedEnemies, out Vector3 spawnPosition)
        {
            Configure(settings);
            var attempts = Mathf.Max(1, settings.MaxSpawnAttemptsPerEnemy);
            for (var i = 0; i < attempts; i++)
            {
                var position = RandomPositionInArea();
                if (!TrySnapToNavMesh(position, out position))
                {
                    continue;
                }

                if (!IsValidSpawnPosition(position, spawnedEnemies))
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

                if (spawnAreaRule.IsNearEdge(position))
                {
                    continue;
                }

                if (IsNearObstacle(position))
                {
                    continue;
                }

                if (IsTooCloseToSpawnedEnemy(position, spawnedEnemies))
                {
                    continue;
                }

                spawnPosition = position;
                return true;
            }

            spawnPosition = Vector3.zero;
            return false;
        }

        public Vector3 ClampToSpawnArea(Vector3 position)
        {
            Configure(settings);
            return spawnAreaRule.Clamp(position);
        }

        public Vector3 SnapToGround(Vector3 position)
        {
            var origin = new Vector3(position.x, position.y + settings.GroundRaycastHeight, position.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, settings.GroundRaycastDistance, settings.GroundLayers, QueryTriggerInteraction.Ignore);
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

                position.y = hit.point.y + settings.GroundOffset;
                return position;
            }

            return position;
        }

        private bool TrySnapToNavMesh(Vector3 position, out Vector3 navPosition)
        {
            Configure(settings);
            position = SnapToGround(position);
            var navSearchRadius = Mathf.Max(0.1f, settings.NavMeshSampleDistance);
            var verticalTolerance = Mathf.Max(0.1f, settings.NavMeshVerticalSampleDistance);
            if (NavMesh.SamplePosition(position, out var hit, navSearchRadius, NavMesh.AllAreas)
                && Mathf.Abs(hit.position.y - position.y) <= verticalTolerance
                && spawnAreaRule.Contains(hit.position))
            {
                navPosition = hit.position;
                return true;
            }

            navPosition = position;
            return spawnAreaRule.Contains(navPosition) && TryGetGroundY(navPosition, out _);
        }

        private bool TryGetBoundedGroundPosition(Vector3 position, out Vector3 groundPosition)
        {
            Configure(settings);
            position = spawnAreaRule.Clamp(position);
            if (TryGetGroundY(position, out var groundY))
            {
                position.y = groundY;
                groundPosition = spawnAreaRule.Clamp(position);
                return true;
            }

            var navSearchRadius = Mathf.Max(0.1f, settings.NavMeshSampleDistance);
            if (NavMesh.SamplePosition(position, out var hit, navSearchRadius, NavMesh.AllAreas))
            {
                groundPosition = spawnAreaRule.Clamp(hit.position);
                return true;
            }

            groundPosition = Vector3.zero;
            return false;
        }

        private bool IsValidSpawnPosition(Vector3 position, IReadOnlyList<DinoEnemy> spawnedEnemies)
        {
            Configure(settings);
            if (!spawnAreaRule.Contains(position))
            {
                return false;
            }

            if (spawnAreaRule.IsNearEdge(position))
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

            return !IsTooCloseToSpawnedEnemy(position, spawnedEnemies);
        }

        private bool IsTooCloseToSpawnedEnemy(Vector3 position, IReadOnlyList<DinoEnemy> spawnedEnemies)
        {
            var minEnemyDistanceSqr = settings.MinDistanceBetweenEnemies * settings.MinDistanceBetweenEnemies;
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
                    return true;
                }
            }

            return false;
        }

        private bool IsTooCloseToPlayerArea(Vector3 position)
        {
            var minDistance = Mathf.Max(0f, settings.MinDistanceFromPlayer);
            var minDistanceSqr = minDistance * minDistance;
            if (settings.Player != null)
            {
                var offset = position - settings.Player.position;
                offset.y = 0f;
                if (offset.sqrMagnitude < minDistanceSqr)
                {
                    return true;
                }
            }

            if (settings.AvoidPlayerStartArea && settings.HasPlayerStartExclusion)
            {
                minDistance = Mathf.Max(minDistance, settings.PlayerStartExclusionRadius);
                minDistanceSqr = minDistance * minDistance;
                var offset = position - settings.PlayerStartExclusionCenter;
                offset.y = 0f;
                if (offset.sqrMagnitude < minDistanceSqr)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsNearObstacle(Vector3 position)
        {
            var radius = Mathf.Max(0.1f, settings.ObstacleSpawnClearance);
            var center = position + Vector3.up * radius;

            if (settings.ObstacleLayers.value != 0
                && HasBlockingObstacleOverlap(center, radius, settings.ObstacleLayers, false))
            {
                return true;
            }

            return HasBlockingObstacleOverlap(center, radius, Physics.AllLayers, true);
        }

        private static bool HasBlockingObstacleOverlap(
            Vector3 center,
            float radius,
            LayerMask layerMask,
            bool requireObstacleName)
        {
            var hitCount = Physics.OverlapSphereNonAlloc(
                center,
                radius,
                ObstacleOverlapHits,
                layerMask,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < hitCount; i++)
            {
                if (IsBlockingSpawnObstacle(ObstacleOverlapHits[i], requireObstacleName))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBlockingSpawnObstacle(Collider targetCollider, bool requireObstacleName)
        {
            if (targetCollider == null || targetCollider.isTrigger)
            {
                return false;
            }

            if (targetCollider.GetComponentInParent<DinoEnemy>() != null
                || targetCollider.GetComponentInParent<PlayerDinoController>() != null)
            {
                return false;
            }

            if (IsGroundCollider(targetCollider) && !IsNamedObstacle(targetCollider))
            {
                return false;
            }

            return !requireObstacleName || IsNamedObstacle(targetCollider);
        }

        private Vector3 RandomPositionInArea()
        {
            Configure(settings);
            return SnapToGround(spawnAreaRule.RandomPosition(
                settings.SpawnY,
                settings.CenterWeightedSpawnChance,
                settings.CenterWeightedSpawnScale,
                () => Random.value,
                Random.Range));
        }

        private bool TryGetGroundY(Vector3 position, out float groundY)
        {
            groundY = position.y;
            var origin = new Vector3(position.x, position.y + settings.GroundRaycastHeight, position.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, settings.GroundRaycastDistance, settings.GroundLayers, QueryTriggerInteraction.Ignore);
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

                groundY = hit.point.y + settings.GroundOffset;
                return true;
            }

            return false;
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
                || targetName.StartsWith("Rock_", System.StringComparison.Ordinal)
                || targetName.StartsWith("SnowRock_", System.StringComparison.Ordinal)
                || targetName.StartsWith("SnowTree_", System.StringComparison.Ordinal)
                || targetName.Contains("Cactus", System.StringComparison.OrdinalIgnoreCase)
                || targetName.Contains("Boulder", System.StringComparison.OrdinalIgnoreCase)
                || targetName.Contains("Stone", System.StringComparison.OrdinalIgnoreCase)
                || targetName.Contains("Cliff", System.StringComparison.OrdinalIgnoreCase)
                || targetName.Contains("Stump", System.StringComparison.OrdinalIgnoreCase)
                || targetName.Contains("Log", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNamedObstacle(Collider targetCollider)
        {
            var target = targetCollider.transform;
            while (target != null)
            {
                if (IsNonGroundSurfaceName(target.name))
                {
                    return true;
                }

                target = target.parent;
            }

            return false;
        }
    }

    public readonly struct EnemySpawnPositionSettings
    {
        public EnemySpawnPositionSettings(
            Vector3 spawnCenter,
            Vector2 spawnSize,
            float spawnY,
            LayerMask groundLayers,
            float groundRaycastHeight,
            float groundRaycastDistance,
            float groundOffset,
            int maxSpawnAttemptsPerEnemy,
            float minDistanceFromPlayer,
            bool avoidPlayerStartArea,
            float playerStartExclusionRadius,
            Vector3 playerStartExclusionCenter,
            bool hasPlayerStartExclusion,
            float minDistanceBetweenEnemies,
            Transform player,
            float navMeshSampleDistance,
            float navMeshVerticalSampleDistance,
            LayerMask obstacleLayers,
            float obstacleSpawnClearance,
            float centerWeightedSpawnChance,
            float centerWeightedSpawnScale,
            float spawnEdgePaddingRatio)
        {
            SpawnCenter = spawnCenter;
            SpawnSize = spawnSize;
            SpawnY = spawnY;
            GroundLayers = groundLayers;
            GroundRaycastHeight = groundRaycastHeight;
            GroundRaycastDistance = groundRaycastDistance;
            GroundOffset = groundOffset;
            MaxSpawnAttemptsPerEnemy = maxSpawnAttemptsPerEnemy;
            MinDistanceFromPlayer = minDistanceFromPlayer;
            AvoidPlayerStartArea = avoidPlayerStartArea;
            PlayerStartExclusionRadius = playerStartExclusionRadius;
            PlayerStartExclusionCenter = playerStartExclusionCenter;
            HasPlayerStartExclusion = hasPlayerStartExclusion;
            MinDistanceBetweenEnemies = minDistanceBetweenEnemies;
            Player = player;
            NavMeshSampleDistance = navMeshSampleDistance;
            NavMeshVerticalSampleDistance = navMeshVerticalSampleDistance;
            ObstacleLayers = obstacleLayers;
            ObstacleSpawnClearance = obstacleSpawnClearance;
            CenterWeightedSpawnChance = centerWeightedSpawnChance;
            CenterWeightedSpawnScale = centerWeightedSpawnScale;
            SpawnEdgePaddingRatio = spawnEdgePaddingRatio;
        }

        public Vector3 SpawnCenter { get; }
        public Vector2 SpawnSize { get; }
        public float SpawnY { get; }
        public LayerMask GroundLayers { get; }
        public float GroundRaycastHeight { get; }
        public float GroundRaycastDistance { get; }
        public float GroundOffset { get; }
        public int MaxSpawnAttemptsPerEnemy { get; }
        public float MinDistanceFromPlayer { get; }
        public bool AvoidPlayerStartArea { get; }
        public float PlayerStartExclusionRadius { get; }
        public Vector3 PlayerStartExclusionCenter { get; }
        public bool HasPlayerStartExclusion { get; }
        public float MinDistanceBetweenEnemies { get; }
        public Transform Player { get; }
        public float NavMeshSampleDistance { get; }
        public float NavMeshVerticalSampleDistance { get; }
        public LayerMask ObstacleLayers { get; }
        public float ObstacleSpawnClearance { get; }
        public float CenterWeightedSpawnChance { get; }
        public float CenterWeightedSpawnScale { get; }
        public float SpawnEdgePaddingRatio { get; }
    }
}
