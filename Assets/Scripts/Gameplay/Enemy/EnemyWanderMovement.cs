using System.Collections.Generic;
using UnityEngine;
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
        [SerializeField] private float fleeDetectDistance = 18f;
        [SerializeField] private float fleeSpeedMultiplier = 1.65f;
        [SerializeField] private float chaseDetectDistance = 16f;
        [SerializeField] private float chaseStopDistance = 22f;
        [SerializeField] private float chaseSpeedMultiplier = 1.45f;
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
        private Transform player;
        private PlayerDinoController playerController;
        private Vector3 moveDirection;
        private float nextDirectionTime;
        private Vector3 desiredMoveDirection;
        private float desiredMoveSpeed;
        private float visualBottomOffset;
        private bool isChasingPlayer;
        private EnemyBehaviorResolver behaviorResolver;

        [Inject]
        public void Construct(EnemyBehaviorResolver behaviorResolver)
        {
            this.behaviorResolver = behaviorResolver;
        }

        public void Configure(
            Vector3 center,
            Vector2 size,
            float speed,
            Transform playerTransform,
            EnemyBehaviorResolver behaviorResolver = null)
        {
            if (this.behaviorResolver == null)
            {
                this.behaviorResolver = behaviorResolver;
            }

            areaCenter = center;
            areaSize = size;
            moveSpeed = speed;
            desiredMoveDirection = Vector3.zero;
            desiredMoveSpeed = 0f;
            CacheVisualBottomOffset();
            transform.position = SnapToGroundImmediate(transform.position);
            SetPlayer(playerTransform);
            ConfigureBody();
            IgnorePlayerSolidCollision();
            PickNewDirection();
        }

        private void Awake()
        {
            enemy = GetComponent<DinoEnemy>();
            body = GetComponent<Rigidbody>();
            UseGroundLayerIfAvailable();
            if (animatorView == null)
            {
                animatorView = GetComponentInChildren<DinoAnimatorView>();
            }

            ConfigureBody();
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
        }

        private void Update()
        {
            var behaviorIntent = ResolveBehaviorIntent(out var playerOffset);
            if (behaviorIntent == EnemyBehaviorIntent.Flee)
            {
                isChasingPlayer = false;
                SetDesiredMove(GetFleeDirection(playerOffset), moveSpeed * fleeSpeedMultiplier);
                animatorView?.SetMove(1f, true);
                return;
            }

            if (behaviorIntent == EnemyBehaviorIntent.Chase)
            {
                isChasingPlayer = true;
                var chaseDirection = GetChaseDirection(playerOffset);
                moveDirection = chaseDirection;
                nextDirectionTime = Time.time + directionChangeInterval;
                SetDesiredMove(chaseDirection, moveSpeed * chaseSpeedMultiplier);
                animatorView?.SetMove(1f, true);
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

            SetDesiredMove(moveDirection, moveSpeed);
            var isRunning = moveSpeed >= runAnimationSpeedThreshold;
            animatorView?.SetMove(isRunning ? 1f : 0.5f, isRunning);
        }

        private void FixedUpdate()
        {
            Move(desiredMoveDirection, desiredMoveSpeed, Time.fixedDeltaTime);
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

        private Vector3 GetFleeDirection(Vector3 playerOffset)
        {
            var fleeDirection = -playerOffset;
            if (fleeDirection.sqrMagnitude <= 0.001f)
            {
                fleeDirection = Random.onUnitSphere;
                fleeDirection.y = 0f;
            }

            fleeDirection.Normalize();
            moveDirection = fleeDirection;
            nextDirectionTime = Time.time + directionChangeInterval;
            return fleeDirection;
        }

        private Vector3 GetChaseDirection(Vector3 playerOffset)
        {
            if (playerOffset.sqrMagnitude <= 0.001f)
            {
                return transform.forward;
            }

            return playerOffset.normalized;
        }

        private void SetDesiredMove(Vector3 direction, float speed)
        {
            desiredMoveDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
            desiredMoveSpeed = Mathf.Max(0f, speed);
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
            if (TryGetInwardDirection(out var inwardDirection))
            {
                moveDirection = inwardDirection;
            }
            else
            {
                var random = Random.insideUnitCircle.normalized;
                moveDirection = new Vector3(random.x, 0f, random.y);
            }

            nextDirectionTime = Time.time + Random.Range(directionChangeInterval * 0.7f, directionChangeInterval * 1.3f);
        }

        private bool IsNearAreaEdge()
        {
            var halfSize = areaSize * 0.5f;
            var local = transform.position - areaCenter;
            return Mathf.Abs(local.x) > halfSize.x * 0.9f || Mathf.Abs(local.z) > halfSize.y * 0.9f;
        }

        private Vector3 ClampToArea(Vector3 position)
        {
            var halfSize = areaSize * 0.5f;
            position.x = Mathf.Clamp(position.x, areaCenter.x - halfSize.x, areaCenter.x + halfSize.x);
            position.z = Mathf.Clamp(position.z, areaCenter.z - halfSize.y, areaCenter.z + halfSize.y);

            return SnapToGround(position);
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
            if (!IsMovingOutOfArea(direction))
            {
                return direction;
            }

            if (TryGetInwardDirection(out var inwardDirection))
            {
                moveDirection = inwardDirection;
                nextDirectionTime = Time.time + directionChangeInterval;
                return inwardDirection;
            }

            return -direction;
        }

        private bool IsMovingOutOfArea(Vector3 direction)
        {
            var halfSize = areaSize * 0.5f;
            var local = transform.position - areaCenter;
            var nearXEdge = Mathf.Abs(local.x) > halfSize.x * 0.88f;
            var nearZEdge = Mathf.Abs(local.z) > halfSize.y * 0.88f;

            if (nearXEdge && Mathf.Sign(local.x) == Mathf.Sign(direction.x))
            {
                return true;
            }

            return nearZEdge && Mathf.Sign(local.z) == Mathf.Sign(direction.z);
        }

        private bool TryGetInwardDirection(out Vector3 inwardDirection)
        {
            if (!IsNearAreaEdge())
            {
                inwardDirection = Vector3.zero;
                return false;
            }

            inwardDirection = areaCenter - transform.position;
            inwardDirection.y = 0f;
            if (inwardDirection.sqrMagnitude <= 0.001f)
            {
                inwardDirection = Vector3.zero;
                return false;
            }

            inwardDirection.Normalize();
            return true;
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
                if (IsWaterCollider(hit.collider))
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
    }
}
