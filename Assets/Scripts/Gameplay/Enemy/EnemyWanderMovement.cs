using System.Collections.Generic;
using DinoGrow.Core.Stage;
using UnityEngine;
using UnityEngine.AI;
using DinoGrow.Core.Enemy;
using DinoGrow.Gameplay.Animation;
using DinoGrow.Gameplay.Player;
using VContainer;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class EnemyWanderMovement : MonoBehaviour
    {
        private static readonly List<EnemyWanderMovement> ActiveMovements = new();
        private static readonly RaycastHit[] ObstacleHits = new RaycastHit[12];
        private static readonly Collider[] ObstacleOverlapHits = new Collider[12];

        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float turnSpeed = 420f;
        [SerializeField] private float directionChangeInterval = 1.6f;
        [SerializeField] private float runAnimationSpeedThreshold = 3f;
        [SerializeField] private float walkAnimationReferenceSpeed = 3.5f;
        [SerializeField] private float runAnimationReferenceSpeed = 6f;
        [SerializeField] private float fleeDetectDistance = 18f;
        [SerializeField] private float fleeSpeedMultiplier = 1.65f;
        [SerializeField] private float chaseDetectDistance = 16f;
        [SerializeField] private float chaseStopDistance = 22f;
        [SerializeField] private float chaseSpeedMultiplier = 2.85f;
        [SerializeField] private float minChaseSpeed = 5.4f;
        [SerializeField] private float maxChaseSpeed = 6.2f;
        [SerializeField] private bool useNavMeshAgent = true;
        [SerializeField] private float navDestinationDistance = 8f;
        [SerializeField] private float navRepathInterval = 0.25f;
        [SerializeField] private float navSampleDistance = 4f;
        [SerializeField] private float navVerticalSampleDistance = 80f;
        [SerializeField] private float groundColliderRadius = 0.45f;
        [SerializeField] private float groundColliderHeight = 1.8f;
        [SerializeField] private float triggerWidth = 1.6f;
        [SerializeField] private float triggerHeight = 2f;
        [SerializeField] private float triggerDepth = 2.4f;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float groundRaycastHeight = 50f;
        [SerializeField] private float groundRaycastDistance = 120f;
        [SerializeField] private float groundOffset = 0f;
        [SerializeField] private float maxGroundSnapStep = 0.2f;
        [SerializeField] private bool avoidObstaclesWithoutNavMesh = true;
        [SerializeField] private LayerMask obstacleLayers = ~0;
        [SerializeField] private bool ignoreOtherEnemies = true;
        [SerializeField] private DinoAnimatorView animatorView;
        [SerializeField] private float aiThinkInterval = 0.18f;
        [SerializeField] private float farAnimationDistance = 36f;
        [SerializeField] private float farAnimationUpdateInterval = 0.25f;
        [SerializeField] private Vector3 areaCenter;
        [SerializeField] private Vector2 areaSize = new(80f, 80f);

        private DinoEnemy enemy;
        private Rigidbody body;
        private NavMeshAgent agent;
        private Transform player;
        private PlayerDinoController playerController;
        private Vector3 moveDirection;
        private float nextDirectionTime;
        private Vector3 desiredMoveDirection;
        private float desiredMoveSpeed;
        private bool isChasingPlayer;
        private float suppressPlayerBehaviorUntil;
        private EnemyBehaviorResolver behaviorResolver;
        private GameStateController gameState;
        private EnemyAnimationMoveRule animationRule;
        private EnemyAreaMovementRule areaRule;
        private EnemyWanderDirectionRule wanderDirectionRule;
        private EnemyBehaviorPlanner behaviorPlanner;
        private EnemyPlayerBehaviorSensor playerBehaviorSensor;
        private EnemyAnimationDriver animationDriver;
        private EnemyNavAgentController navAgentController;
        private EnemyGroundProbe groundProbe;
        private float nextAiThinkTime;
        private float obstacleCastRadius;
        private float obstacleCastHeight;
        private Vector3 obstacleCastCenter;
        private EnemyBehaviorIntent cachedBehaviorIntent = EnemyBehaviorIntent.Wander;
        private Vector3 cachedPlayerOffset;

        [Inject]
        public void Construct(EnemyBehaviorResolver behaviorResolver, GameStateController gameState)
        {
            this.behaviorResolver = behaviorResolver;
            this.gameState = gameState;
            RebuildPlayerBehaviorSensor();
        }

        public void Configure(
            Vector3 center,
            Vector2 size,
            float speed,
            Transform playerTransform,
            EnemyBehaviorResolver behaviorResolver = null,
            GameStateController gameState = null)
        {
            if (this.behaviorResolver == null)
            {
                this.behaviorResolver = behaviorResolver;
            }

            if (this.gameState == null)
            {
                this.gameState = gameState;
            }

            RebuildPlayerBehaviorSensor();
            areaCenter = center;
            areaSize = size;
            moveSpeed = speed;
            desiredMoveDirection = Vector3.zero;
            desiredMoveSpeed = 0f;
            nextAiThinkTime = Time.time + Random.Range(0f, Mathf.Max(0.01f, aiThinkInterval));
            ConfigureRules();
            ConfigureGroundProbe();
            groundProbe.CacheVisualBottomOffset();
            transform.position = groundProbe.SnapImmediate(transform.position);
            SetPlayer(playerTransform);
            ConfigureBody();
            ConfigureAgent();
            IgnorePlayerSolidCollision();
            PickNewDirection();
        }

        private void Awake()
        {
            enemy = GetComponent<DinoEnemy>();
            body = GetComponent<Rigidbody>();
            agent = GetComponent<NavMeshAgent>();
            UseGroundLayerIfAvailable();
            if (animatorView == null)
            {
                animatorView = GetComponentInChildren<DinoAnimatorView>();
            }

            ConfigureRules();
            ConfigureBody();
            ConfigureAgent();
        }

        private void ConfigureRules()
        {
            if (animationRule == null)
            {
                animationRule = new EnemyAnimationMoveRule(
                    runAnimationSpeedThreshold,
                    walkAnimationReferenceSpeed,
                    runAnimationReferenceSpeed);
            }

            areaRule ??= new EnemyAreaMovementRule(areaCenter, areaSize);
            wanderDirectionRule ??= new EnemyWanderDirectionRule(directionChangeInterval);
            behaviorPlanner ??= new EnemyBehaviorPlanner(
                    fleeSpeedMultiplier,
                    chaseSpeedMultiplier,
                    animationRule,
                    minChaseSpeed,
                    maxChaseSpeed);
            playerBehaviorSensor ??= CreatePlayerBehaviorSensor();
            animationDriver ??= new EnemyAnimationDriver(
                animatorView,
                animationRule,
                transform,
                farAnimationDistance,
                farAnimationUpdateInterval);
            navAgentController ??= new EnemyNavAgentController(gameObject, transform, agent);
            groundProbe ??= new EnemyGroundProbe(transform);
        }

        private void ConfigureGroundProbe()
        {
            groundProbe ??= new EnemyGroundProbe(transform);
            groundProbe.Configure(
                groundLayers,
                areaCenter,
                groundRaycastHeight,
                groundRaycastDistance,
                groundOffset,
                maxGroundSnapStep);
        }

        private void RebuildPlayerBehaviorSensor()
        {
            playerBehaviorSensor = CreatePlayerBehaviorSensor();
        }

        private EnemyPlayerBehaviorSensor CreatePlayerBehaviorSensor()
        {
            return new EnemyPlayerBehaviorSensor(
                behaviorResolver,
                fleeDetectDistance,
                chaseDetectDistance,
                chaseStopDistance);
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

        private void Start()
        {
            PickNewDirection();
        }

        private void OnEnable()
        {
            if (!ActiveMovements.Contains(this))
            {
                ActiveMovements.Add(this);
            }

            IgnoreEnemyCollisions();
        }

        private void OnDisable()
        {
            ActiveMovements.Remove(this);
            navAgentController?.ResetPath();
        }

        private void Update()
        {
            if (gameState == null || !gameState.IsPlaying)
            {
                SetDesiredMove(Vector3.zero, 0f);
                animationDriver?.Stop();
                return;
            }

            ConfigureRules();

            if (Time.time < suppressPlayerBehaviorUntil)
            {
                SetDesiredMove(Vector3.zero, 0f);
                animationDriver?.StopMoveOnly();
                return;
            }

            UpdateBehaviorIntentIfNeeded();
            UpdateAnimationDetailLevel();
            if (TryApplyPlayerBehavior(cachedBehaviorIntent, cachedPlayerOffset))
            {
                return;
            }

            if (isChasingPlayer)
            {
                isChasingPlayer = false;
                PickNewDirection();
            }

            if (Time.time >= nextDirectionTime || IsNearAreaEdge())
            {
                PickNewDirection();
            }

            ApplyMovementPlan(behaviorPlanner.Plan(
                EnemyBehaviorIntent.Wander,
                cachedPlayerOffset,
                moveDirection,
                transform.forward,
                moveSpeed));
        }

        private void UpdateBehaviorIntentIfNeeded()
        {
            if (Time.time < nextAiThinkTime)
            {
                return;
            }

            nextAiThinkTime = Time.time + Mathf.Max(0.03f, aiThinkInterval);
            cachedBehaviorIntent = ResolveBehaviorIntent(out cachedPlayerOffset);
        }

        private void UpdateAnimationDetailLevel()
        {
            animationDriver?.UpdateDetailLevel(player);
        }

        private bool TryApplyPlayerBehavior(EnemyBehaviorIntent behaviorIntent, Vector3 playerOffset)
        {
            if (behaviorIntent == EnemyBehaviorIntent.Wander)
            {
                return false;
            }

            var plan = behaviorPlanner.Plan(
                behaviorIntent,
                playerOffset,
                moveDirection,
                transform.forward,
                moveSpeed);
            if (behaviorIntent == EnemyBehaviorIntent.Flee)
            {
                isChasingPlayer = false;
                moveDirection = plan.Direction;
                nextDirectionTime = Time.time + directionChangeInterval;
                ApplyMovementPlan(plan);
                return true;
            }

            isChasingPlayer = true;
            moveDirection = plan.Direction;
            nextDirectionTime = Time.time + directionChangeInterval;
            SetDesiredMove(plan.Direction, plan.Speed);
            ApplyAgentDestination(player.position, plan.Speed);
            ApplyAnimationPlan(plan);
            return true;
        }

        private void ApplyMovementPlan(EnemyMovementPlan plan)
        {
            SetDesiredMove(plan.Direction, plan.Speed);
            ApplyAnimationPlan(plan);
        }

        private void ApplyAnimationPlan(EnemyMovementPlan plan)
        {
            animationDriver?.ApplyMovementPlan(plan);
        }

        private void FixedUpdate()
        {
            if (gameState == null || !gameState.IsPlaying)
            {
                StopBody();
                navAgentController?.ResetPath();

                return;
            }

            if (navAgentController != null && navAgentController.CanUse())
            {
                if (navAgentController.IsStalled(desiredMoveDirection, desiredMoveSpeed))
                {
                    navAgentController.Disable();
                    Move(desiredMoveDirection, desiredMoveSpeed, Time.fixedDeltaTime);
                    return;
                }

                if (body != null)
                {
                    body.position = transform.position;
                }

                StopBody();
                return;
            }

            Move(desiredMoveDirection, desiredMoveSpeed, Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            navAgentController?.RotateWithVelocity(turnSpeed);
        }

        public void OnPlayerBitten(float idleDuration = 0.75f)
        {
            isChasingPlayer = false;
            suppressPlayerBehaviorUntil = Time.time + Mathf.Max(0f, idleDuration);
            SetDesiredMove(Vector3.zero, 0f);
            animationDriver?.Stop();
            nextDirectionTime = suppressPlayerBehaviorUntil;
            PickNewDirection();
        }

        private void SetPlayer(Transform playerTransform)
        {
            player = playerTransform;
            playerController = player != null ? player.GetComponent<PlayerDinoController>() : null;
            IgnorePlayerSolidCollision();
        }

        private EnemyBehaviorIntent ResolveBehaviorIntent(out Vector3 playerOffset)
        {
            ConfigureRules();
            return playerBehaviorSensor.Resolve(
                enemy,
                transform,
                player,
                playerController,
                isChasingPlayer,
                out playerOffset);
        }

        private void SetDesiredMove(Vector3 direction, float speed)
        {
            desiredMoveDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
            desiredMoveSpeed = Mathf.Max(0f, speed);
            ApplyAgentDestination();
        }

        private void Move(Vector3 direction, float speed, float deltaTime)
        {
            if (direction.sqrMagnitude <= 0.001f)
            {
                StopBody();

                return;
            }

            direction = GetAreaSafeDirection(direction);
            var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            var nextRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * deltaTime);

            if (body != null)
            {
                body.MoveRotation(nextRotation);
                body.MovePosition(GetSafeMovePosition(body.position, direction, speed * deltaTime));
                StopBody();
            }
            else
            {
                transform.rotation = nextRotation;
                transform.position = GetSafeMovePosition(transform.position, direction, speed * deltaTime);
            }
        }

        private void ConfigureBody()
        {
            if (body == null)
            {
                return;
            }

            ConfigureGroundProbe();
            groundProbe.CacheVisualBottomOffset();
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            StopBody();
            EnsureSolidCollider();
            IgnoreEnemyCollisions();
            IgnorePlayerSolidCollision();
        }

        private void ConfigureAgent()
        {
            ConfigureRules();
            useNavMeshAgent = navAgentController != null
                && navAgentController.Configure(
                    useNavMeshAgent,
                    moveSpeed,
                    turnSpeed,
                    groundColliderRadius,
                    groundColliderHeight,
                    GetAgentBaseOffset(),
                    navSampleDistance,
                    navVerticalSampleDistance);
            agent = navAgentController?.Agent;
        }

        private float GetAgentBaseOffset()
        {
            ConfigureGroundProbe();
            groundProbe.CacheVisualBottomOffset();
            return Mathf.Abs(groundProbe.VisualBottomOffset) <= 0.001f
                ? 0f
                : -groundProbe.VisualBottomOffset;
        }

        private void ApplyAgentDestination()
        {
            navAgentController?.ApplyMoveDestination(
                transform.position,
                desiredMoveDirection,
                desiredMoveSpeed,
                navDestinationDistance,
                navSampleDistance,
                navRepathInterval,
                ClampToArea);
            useNavMeshAgent = navAgentController != null && navAgentController.CanUse();
        }

        private void ApplyAgentDestination(Vector3 destination, float speed)
        {
            navAgentController?.ApplyDestination(
                ClampToArea(destination),
                speed,
                navSampleDistance,
                navRepathInterval);
            useNavMeshAgent = navAgentController != null && navAgentController.CanUse();
        }

        private void StopBody()
        {
            if (body == null || body.isKinematic)
            {
                return;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        private void EnsureSolidCollider()
        {
            foreach (var meshCollider in GetComponents<MeshCollider>())
            {
                meshCollider.enabled = false;
            }

            var localBounds = CalculateLocalVisualBounds();
            var colliderRadius = groundColliderRadius;
            var colliderHeight = groundColliderHeight;
            var colliderCenter = new Vector3(0f, colliderHeight * 0.5f, 0f);
            var triggerSize = new Vector3(triggerWidth, triggerHeight, triggerDepth);
            var triggerCenter = new Vector3(0f, triggerHeight * 0.5f, 0f);

            if (localBounds.HasValue)
            {
                var bounds = localBounds.Value;
                colliderRadius = Mathf.Max(groundColliderRadius, Mathf.Min(bounds.size.x, bounds.size.z) * 0.22f);
                colliderHeight = Mathf.Max(groundColliderHeight, bounds.size.y * 0.85f);
                colliderCenter = new Vector3(bounds.center.x, bounds.min.y + colliderHeight * 0.5f, bounds.center.z);
                triggerSize = new Vector3(
                    Mathf.Max(triggerWidth, bounds.size.x * 0.75f),
                    Mathf.Max(triggerHeight, bounds.size.y),
                    Mathf.Max(triggerDepth, bounds.size.z * 0.75f));
                triggerCenter = bounds.center;
            }

            var capsule = GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = gameObject.AddComponent<CapsuleCollider>();
            }

            capsule.isTrigger = false;
            capsule.direction = 1;
            capsule.radius = colliderRadius;
            capsule.height = Mathf.Max(colliderHeight, colliderRadius * 2f);
            capsule.center = colliderCenter;
            obstacleCastRadius = capsule.radius;
            obstacleCastHeight = capsule.height;
            obstacleCastCenter = capsule.center;

            var trigger = GetComponent<BoxCollider>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<BoxCollider>();
            }

            trigger.isTrigger = true;
            trigger.center = triggerCenter;
            trigger.size = triggerSize;
        }

        private Bounds? CalculateLocalVisualBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            var hasBounds = false;
            var localMin = Vector3.zero;
            var localMax = Vector3.zero;

            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer.GetComponent<TextMesh>() != null)
                {
                    continue;
                }

                var bounds = targetRenderer.bounds;
                var corners = new[]
                {
                    new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
                    new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                    new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                    new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                    new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                    new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                    new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                    new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)
                };

                foreach (var corner in corners)
                {
                    var local = transform.InverseTransformPoint(corner);
                    if (!hasBounds)
                    {
                        localMin = local;
                        localMax = local;
                        hasBounds = true;
                    }
                    else
                    {
                        localMin = Vector3.Min(localMin, local);
                        localMax = Vector3.Max(localMax, local);
                    }
                }
            }

            if (!hasBounds)
            {
                return null;
            }

            var result = new Bounds((localMin + localMax) * 0.5f, localMax - localMin);
            return result;
        }

        private void IgnoreEnemyCollisions()
        {
            if (!ignoreOtherEnemies)
            {
                return;
            }

            var ownColliders = GetComponents<Collider>();
            foreach (var otherMovement in ActiveMovements)
            {
                if (otherMovement == null || otherMovement == this)
                {
                    continue;
                }

                var otherColliders = otherMovement.GetComponents<Collider>();
                foreach (var ownCollider in ownColliders)
                {
                    if (ownCollider == null || ownCollider.isTrigger)
                    {
                        continue;
                    }

                    foreach (var otherCollider in otherColliders)
                    {
                        if (otherCollider == null || otherCollider.isTrigger)
                        {
                            continue;
                        }

                        Physics.IgnoreCollision(ownCollider, otherCollider, true);
                    }
                }
            }
        }

        private void IgnorePlayerSolidCollision()
        {
            if (player == null)
            {
                return;
            }

            var ownColliders = GetComponents<Collider>();
            var playerColliders = player.GetComponents<Collider>();
            foreach (var ownCollider in ownColliders)
            {
                if (ownCollider == null || ownCollider.isTrigger)
                {
                    continue;
                }

                foreach (var playerCollider in playerColliders)
                {
                    if (playerCollider == null || playerCollider.isTrigger)
                    {
                        continue;
                    }

                    Physics.IgnoreCollision(ownCollider, playerCollider, true);
                }
            }
        }

        private void PickNewDirection()
        {
            ConfigureRules();
            areaRule.Configure(areaCenter, areaSize);
            moveDirection = wanderDirectionRule.PickDirection(transform.position, areaRule);
            nextDirectionTime = wanderDirectionRule.GetNextDirectionTime(Time.time);
        }

        private bool IsNearAreaEdge()
        {
            ConfigureRules();
            areaRule.Configure(areaCenter, areaSize);
            return areaRule.IsNearEdge(transform.position);
        }

        private Vector3 ClampToArea(Vector3 position)
        {
            ConfigureRules();
            areaRule.Configure(areaCenter, areaSize);
            ConfigureGroundProbe();
            return groundProbe.Snap(areaRule.Clamp(position));
        }

        private Vector3 GetSafeMovePosition(Vector3 currentPosition, Vector3 direction, float distance)
        {
            var nextPosition = currentPosition + direction * distance;
            nextPosition = ClampToArea(nextPosition);
            ConfigureGroundProbe();
            if (!groundProbe.IsWaterAt(nextPosition))
            {
                if (ShouldAvoidObstaclesWithoutNavMesh()
                    && TryGetObstacleEscapePosition(currentPosition, out var escapePosition))
                {
                    PickNewDirection();
                    return groundProbe.Snap(ClampToArea(escapePosition));
                }

                if (ShouldAvoidObstaclesWithoutNavMesh()
                    && IsObstacleInMove(currentPosition, direction, distance, out var hit))
                {
                    var slideDirection = Vector3.ProjectOnPlane(direction, hit.normal);
                    slideDirection.y = 0f;
                    if (slideDirection.sqrMagnitude > 0.001f
                        && !IsObstacleInMove(currentPosition, slideDirection.normalized, distance, out _))
                    {
                        moveDirection = slideDirection.normalized;
                        nextDirectionTime = Time.time + directionChangeInterval;
                        return groundProbe.Snap(ClampToArea(currentPosition + moveDirection * distance));
                    }

                    PickNewDirection();
                    return groundProbe.Snap(currentPosition);
                }

                return nextPosition;
            }

            if (TryGetInwardDirection(out var inwardDirection))
            {
                moveDirection = inwardDirection;
                nextDirectionTime = Time.time + directionChangeInterval;
            }
            else
            {
                PickNewDirection();
            }

            return groundProbe.Snap(currentPosition);
        }

        private Vector3 GetAreaSafeDirection(Vector3 direction)
        {
            ConfigureRules();
            areaRule.Configure(areaCenter, areaSize);
            var safeDirection = areaRule.GetSafeDirection(transform.position, direction);
            if (safeDirection == direction)
            {
                return direction;
            }

            if (safeDirection.sqrMagnitude > 0.001f)
            {
                moveDirection = safeDirection;
                nextDirectionTime = Time.time + directionChangeInterval;
                return safeDirection;
            }

            return direction;
        }

        private bool TryGetInwardDirection(out Vector3 inwardDirection)
        {
            ConfigureRules();
            areaRule.Configure(areaCenter, areaSize);
            return areaRule.TryGetInwardDirection(transform.position, out inwardDirection);
        }

        private bool IsObstacleInMove(Vector3 currentPosition, Vector3 direction, float distance, out RaycastHit hit)
        {
            hit = default;
            if (direction.sqrMagnitude <= 0.001f || distance <= 0f)
            {
                return false;
            }

            GetObstacleCapsule(currentPosition, out var point1, out var point2, out var radius);
            var castDistance = distance + radius;
            var hitCount = Physics.CapsuleCastNonAlloc(
                point1,
                point2,
                radius,
                direction.normalized,
                ObstacleHits,
                castDistance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);

            if (hitCount == 0)
            {
                return false;
            }

            System.Array.Sort(ObstacleHits, 0, hitCount, RaycastHitDistanceComparer.Instance);
            for (var i = 0; i < hitCount; i++)
            {
                var candidate = ObstacleHits[i];
                if (!IsBlockingObstacle(candidate.collider))
                {
                    continue;
                }

                hit = candidate;
                return true;
            }

            return false;
        }

        private bool TryGetObstacleEscapePosition(Vector3 currentPosition, out Vector3 escapePosition)
        {
            GetObstacleCapsule(currentPosition, out var point1, out var point2, out var radius);
            var hitCount = Physics.OverlapCapsuleNonAlloc(
                point1,
                point2,
                radius,
                ObstacleOverlapHits,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < hitCount; i++)
            {
                var targetCollider = ObstacleOverlapHits[i];
                if (!IsBlockingObstacle(targetCollider))
                {
                    continue;
                }

                var closestPoint = targetCollider.ClosestPoint(currentPosition);
                var away = currentPosition - closestPoint;
                away.y = 0f;
                if (away.sqrMagnitude <= 0.001f)
                {
                    away = currentPosition - targetCollider.bounds.center;
                    away.y = 0f;
                }

                if (away.sqrMagnitude <= 0.001f)
                {
                    away = -transform.forward;
                    away.y = 0f;
                }

                escapePosition = currentPosition + away.normalized * Mathf.Max(0.25f, radius);
                return true;
            }

            escapePosition = currentPosition;
            return false;
        }

        private void GetObstacleCapsule(Vector3 rootPosition, out Vector3 point1, out Vector3 point2, out float radius)
        {
            radius = Mathf.Max(0.1f, obstacleCastRadius > 0f ? obstacleCastRadius : groundColliderRadius);
            var height = Mathf.Max(radius * 2f, obstacleCastHeight > 0f ? obstacleCastHeight : groundColliderHeight);
            var center = rootPosition + (obstacleCastCenter == Vector3.zero
                ? Vector3.up * (height * 0.5f)
                : obstacleCastCenter);
            var halfSegment = Mathf.Max(0f, (height * 0.5f) - radius);
            point1 = center + Vector3.up * halfSegment;
            point2 = center - Vector3.up * halfSegment;
        }

        private bool ShouldAvoidObstaclesWithoutNavMesh()
        {
            return avoidObstaclesWithoutNavMesh || !useNavMeshAgent;
        }

        private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
        {
            public static readonly RaycastHitDistanceComparer Instance = new();

            public int Compare(RaycastHit left, RaycastHit right)
            {
                return left.distance.CompareTo(right.distance);
            }
        }

        private bool IsBlockingObstacle(Collider targetCollider)
        {
            if (targetCollider == null || targetCollider.isTrigger)
            {
                return false;
            }

            if (targetCollider.GetComponentInParent<DinoEnemy>() != null)
            {
                return false;
            }

            if (targetCollider.GetComponentInParent<PlayerDinoController>() != null)
            {
                return false;
            }

            return IsNamedObstacle(targetCollider);
        }

        private static bool IsNamedObstacle(Collider targetCollider)
        {
            var target = targetCollider.transform;
            while (target != null)
            {
                if (target.name == "MapBoundary"
                    || target.name.StartsWith("Tree_", System.StringComparison.Ordinal)
                    || target.name.StartsWith("Rock_", System.StringComparison.Ordinal)
                    || target.name.StartsWith("SnowRock_", System.StringComparison.Ordinal)
                    || target.name.StartsWith("SnowTree_", System.StringComparison.Ordinal)
                    || target.name.Contains("Tree", System.StringComparison.OrdinalIgnoreCase)
                    || target.name.Contains("Rock", System.StringComparison.OrdinalIgnoreCase)
                    || target.name.Contains("Cactus", System.StringComparison.OrdinalIgnoreCase)
                    || target.name.Contains("Boulder", System.StringComparison.OrdinalIgnoreCase)
                    || target.name.Contains("Stone", System.StringComparison.OrdinalIgnoreCase)
                    || target.name.Contains("Cliff", System.StringComparison.OrdinalIgnoreCase)
                    || target.name.Contains("Stump", System.StringComparison.OrdinalIgnoreCase)
                    || target.name.Contains("Log", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                target = target.parent;
            }

            return false;
        }
    }
}
