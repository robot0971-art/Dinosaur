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
        [SerializeField] private float chaseSpeedMultiplier = 1.45f;
        [SerializeField] private bool useNavMeshAgent = true;
        [SerializeField] private float navDestinationDistance = 8f;
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
        [SerializeField] private bool ignoreOtherEnemies = true;
        [SerializeField] private DinoAnimatorView animatorView;
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
        private float visualBottomOffset;
        private bool isChasingPlayer;
        private float suppressPlayerBehaviorUntil;
        private EnemyBehaviorResolver behaviorResolver;
        private GameStateController gameState;
        private EnemyAnimationMoveRule animationRule;
        private EnemyAreaMovementRule areaRule;
        private EnemyWanderDirectionRule wanderDirectionRule;
        private EnemyBehaviorPlanner behaviorPlanner;

        [Inject]
        public void Construct(EnemyBehaviorResolver behaviorResolver, GameStateController gameState)
        {
            this.behaviorResolver = behaviorResolver;
            this.gameState = gameState;
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

            areaCenter = center;
            areaSize = size;
            moveSpeed = speed;
            desiredMoveDirection = Vector3.zero;
            desiredMoveSpeed = 0f;
            ConfigureRules();
            CacheVisualBottomOffset();
            transform.position = SnapToGroundImmediate(transform.position);
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
                    animationRule);
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
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
        }

        private void Update()
        {
            if (gameState == null || !gameState.IsPlaying)
            {
                SetDesiredMove(Vector3.zero, 0f);
                animatorView?.SetMove(0f, false);
                animatorView?.SetPlaybackSpeed(1f);
                return;
            }

            ConfigureRules();

            if (Time.time < suppressPlayerBehaviorUntil)
            {
                SetDesiredMove(Vector3.zero, 0f);
                animatorView?.SetMove(0f, false);
                return;
            }

            var behaviorIntent = ResolveBehaviorIntent(out var playerOffset);
            if (TryApplyPlayerBehavior(behaviorIntent, playerOffset))
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
                playerOffset,
                moveDirection,
                transform.forward,
                moveSpeed));
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
            animatorView?.SetMove(animationRule.GetMoveBlend(plan.Speed, plan.IsRunning), plan.IsRunning);
            animatorView?.SetPlaybackSpeed(animationRule.GetPlaybackSpeed(plan.Speed, plan.IsRunning));
        }

        private void FixedUpdate()
        {
            if (gameState == null || !gameState.IsPlaying)
            {
                StopBody();
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                }

                return;
            }

            if (CanUseAgent())
            {
                if (IsAgentStalled())
                {
                    DisableAgentForManualMovement();
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
            RotateWithAgentVelocity();
        }

        public void OnPlayerBitten(float idleDuration = 0.75f)
        {
            isChasingPlayer = false;
            suppressPlayerBehaviorUntil = Time.time + Mathf.Max(0f, idleDuration);
            SetDesiredMove(Vector3.zero, 0f);
            animatorView?.SetMove(0f, false);
            animatorView?.SetPlaybackSpeed(1f);
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
            playerOffset = Vector3.zero;
            if (behaviorResolver == null || enemy == null || player == null || playerController == null)
            {
                return EnemyBehaviorIntent.Wander;
            }

            playerOffset = player.position - transform.position;
            playerOffset.y = 0f;

            return behaviorResolver.Resolve(
                enemy.Level,
                playerController.Level,
                playerOffset.magnitude,
                fleeDetectDistance,
                chaseDetectDistance,
                chaseStopDistance,
                isChasingPlayer);
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

            CacheVisualBottomOffset();
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
            if (!useNavMeshAgent)
            {
                if (agent != null)
                {
                    agent.enabled = false;
                }

                return;
            }

            var navSearchRadius = Mathf.Max(navSampleDistance, navVerticalSampleDistance);
            if (!NavMesh.SamplePosition(transform.position, out var navHit, navSearchRadius, NavMesh.AllAreas))
            {
                useNavMeshAgent = false;
                if (agent != null)
                {
                    agent.enabled = false;
                }

                return;
            }

            transform.position = navHit.position;

            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    agent = gameObject.AddComponent<NavMeshAgent>();
                }
            }

            if (agent == null)
            {
                useNavMeshAgent = false;
                return;
            }

            agent.speed = Mathf.Max(0.1f, moveSpeed);
            agent.angularSpeed = turnSpeed;
            agent.acceleration = Mathf.Max(8f, moveSpeed * 4f);
            agent.stoppingDistance = 0f;
            agent.autoBraking = false;
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.radius = Mathf.Max(0.1f, groundColliderRadius);
            agent.height = Mathf.Max(agent.radius * 2f, groundColliderHeight);
            agent.baseOffset = GetAgentBaseOffset();
            agent.enabled = true;
            agent.Warp(navHit.position);
        }

        private float GetAgentBaseOffset()
        {
            CacheVisualBottomOffset();
            return Mathf.Abs(visualBottomOffset) <= 0.001f
                ? 0f
                : -visualBottomOffset;
        }

        private bool CanUseAgent()
        {
            return useNavMeshAgent && agent != null && agent.enabled && agent.isOnNavMesh;
        }

        private void ApplyAgentDestination()
        {
            if (!CanUseAgent())
            {
                return;
            }

            agent.speed = Mathf.Max(0.1f, desiredMoveSpeed);
            if (desiredMoveDirection.sqrMagnitude <= 0.001f || desiredMoveSpeed <= 0.001f)
            {
                agent.ResetPath();
                return;
            }

            var destination = transform.position + desiredMoveDirection * navDestinationDistance;
            destination = ClampToArea(destination);
            if (!NavMesh.SamplePosition(destination, out var hit, navSampleDistance, NavMesh.AllAreas))
            {
                DisableAgentForManualMovement();
                return;
            }

            if (!agent.SetDestination(hit.position))
            {
                DisableAgentForManualMovement();
            }
        }

        private void ApplyAgentDestination(Vector3 destination, float speed)
        {
            if (!CanUseAgent())
            {
                return;
            }

            agent.speed = Mathf.Max(0.1f, speed);
            destination = ClampToArea(destination);
            if (NavMesh.SamplePosition(destination, out var hit, navSampleDistance, NavMesh.AllAreas))
            {
                if (!agent.SetDestination(hit.position))
                {
                    DisableAgentForManualMovement();
                }
                return;
            }

            DisableAgentForManualMovement();
        }

        private void DisableAgentForManualMovement()
        {
            useNavMeshAgent = false;
            if (agent != null)
            {
                agent.ResetPath();
                agent.enabled = false;
            }
        }

        private bool IsAgentStalled()
        {
            if (!CanUseAgent() || desiredMoveDirection.sqrMagnitude <= 0.001f || desiredMoveSpeed <= 0.001f)
            {
                return false;
            }

            if (!agent.hasPath && !agent.pathPending)
            {
                return true;
            }

            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                return true;
            }

            var velocity = agent.velocity;
            velocity.y = 0f;
            return !agent.pathPending
                && agent.remainingDistance > agent.stoppingDistance + 0.1f
                && velocity.sqrMagnitude <= 0.0001f;
        }

        private void RotateWithAgentVelocity()
        {
            if (!CanUseAgent())
            {
                return;
            }

            var velocity = agent.desiredVelocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude <= 0.001f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
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
            return SnapToGround(areaRule.Clamp(position));
        }

        private Vector3 SnapToGroundImmediate(Vector3 position)
        {
            if (TryGetGroundY(position, out var targetY))
            {
                position.y = targetY - visualBottomOffset;
            }

            return position;
        }

        private Vector3 GetSafeMovePosition(Vector3 currentPosition, Vector3 direction, float distance)
        {
            var nextPosition = currentPosition + direction * distance;
            nextPosition = ClampToArea(nextPosition);
            if (!IsWaterAt(nextPosition))
            {
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

            return SnapToGround(currentPosition);
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

        private Vector3 SnapToGround(Vector3 position)
        {
            if (TryGetGroundY(position, out var targetY))
            {
                position.y = Mathf.MoveTowards(position.y, targetY - visualBottomOffset, maxGroundSnapStep);
            }

            return position;
        }

        private void CacheVisualBottomOffset()
        {
            var bounds = CalculateWorldVisualBounds();
            visualBottomOffset = bounds.HasValue
                ? bounds.Value.min.y - transform.position.y
                : 0f;
        }

        private Bounds? CalculateWorldVisualBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            var hasBounds = false;
            var bounds = new Bounds(transform.position, Vector3.zero);

            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer.GetComponent<TextMesh>() != null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = targetRenderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(targetRenderer.bounds);
            }

            return hasBounds ? bounds : null;
        }

        private bool TryGetGroundY(Vector3 position, out float groundY)
        {
            groundY = position.y;
            var originY = Mathf.Max(position.y + groundRaycastHeight, areaCenter.y + groundRaycastHeight);
            var origin = new Vector3(position.x, originY, position.z);
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

                if (hit.collider.GetComponentInParent<DinoEnemy>() != null)
                {
                    continue;
                }

                if (hit.collider.GetComponentInParent<PlayerDinoController>() != null)
                {
                    continue;
                }

                groundY = hit.point.y + groundOffset;
                return true;
            }

            return false;
        }

        private bool IsWaterAt(Vector3 position)
        {
            var originY = Mathf.Max(position.y + groundRaycastHeight, areaCenter.y + groundRaycastHeight);
            var origin = new Vector3(position.x, originY, position.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, groundRaycastDistance, groundLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                return false;
            }

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<DinoEnemy>() != null)
                {
                    continue;
                }

                if (hit.collider.GetComponentInParent<PlayerDinoController>() != null)
                {
                    continue;
                }

                return IsWaterCollider(hit.collider);
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

        private static bool IsWaterCollider(Collider targetCollider)
        {
            if (targetCollider == null)
            {
                return false;
            }

            var target = targetCollider.transform;
            while (target != null)
            {
                if (target.name == "Water")
                {
                    return true;
                }

                target = target.parent;
            }

            return false;
        }

        private static bool IsNonGroundSurfaceName(string targetName)
        {
            return targetName == "Water"
                || targetName == "MapBoundary"
                || targetName.StartsWith("Tree_", System.StringComparison.Ordinal)
                || targetName.StartsWith("Rock_", System.StringComparison.Ordinal);
        }
    }
}
