using DinoGrow.Core.Combat;
using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;
using DinoGrow.Gameplay;
using DinoGrow.Gameplay.Animation;
using DinoGrow.Gameplay.Enemy;
using DinoGrow.Infrastructure.Data;
using DinoGrow.Infrastructure.DI;
using DinoGrow.Infrastructure.Events;
using UnityEngine;
using VContainer;

namespace DinoGrow.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerDinoController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 9f;
        [SerializeField] private float sprintMultiplier = 1.6f;
        [SerializeField] private float turnSpeed = 540f;
        [SerializeField] private string playerDataId = "player";
        [SerializeField] private bool useDataSize;
        [SerializeField] private bool applyGrowthScale = true;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float groundRaycastHeight = 20f;
        [SerializeField] private float groundRaycastDistance = 60f;
        [SerializeField] private float groundOffset = 0f;
        [SerializeField] private float visualGroundOffset = 0f;
        [SerializeField] private float maxGroundSnapStep = 0.18f;
        [SerializeField] private LayerMask obstacleLayers = ~0;
        [SerializeField] private float obstacleSkinWidth = 0.08f;
        [SerializeField] private float maxObstacleCorrectionStep = 0.18f;
        [SerializeField] private float minObstacleRadius = 0.35f;
        [SerializeField] private float minObstacleHeight = 1.4f;
        [SerializeField] private Rigidbody body;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private DinoAnimatorView animatorView;
        [SerializeField] private Transform mouthEffectOrigin;
        [SerializeField] private Vector3 mouthEffectFallbackOffset = new Vector3(0f, 1.05f, 0.75f);
        [SerializeField] private float enemyContactRadius = 1.35f;
        [SerializeField] private LayerMask enemyContactLayers = ~0;
        [SerializeField] private float eatContactCooldown = 0.18f;
        [SerializeField] private bool useMovementBounds;
        [SerializeField] private Vector3 movementBoundsCenter;
        [SerializeField] private Vector2 movementBoundsSize = new(80f, 80f);
        private EatResolver eatResolver;
        private GrowthSystem growthSystem;
        private PlayerProgress progress;
        private GameStateController gameState;
        private StageRule stageRule;
        private DeathEffectService deathEffectService;
        private GameEventBus eventBus;
        private DinoDataRepository dinoDataRepository;
        private PlayerGrowthDataRepository playerGrowthDataRepository;
        private Vector2 rotateInput;
        private bool isSprinting;
        private Vector3 baseVisualScale = Vector3.one;
        private Vector3 baseVisualLocalPosition;
        private bool isDead;
        private bool dependenciesReady;
        private float visualBottomOffset;
        private float obstacleRadius = 0.45f;
        private float obstacleHeight = 1.8f;
        private Vector3 obstacleCenterOffset = new Vector3(0f, 0.9f, 0f);
        private DinoEnemy resolvingEnemy;
        private float nextEatContactTime;

        public int Level => progress?.Level ?? 1;

        public void SetMovementBounds(Vector3 center, Vector2 size)
        {
            movementBoundsCenter = center;
            movementBoundsSize = new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
            useMovementBounds = true;
        }

        public void ClearMovementBounds()
        {
            useMovementBounds = false;
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            if (body != null)
            {
                body.position = position;
                body.rotation = rotation;
                StopBody();
            }

            rotateInput = Vector2.zero;
            isSprinting = false;
            animatorView?.SetMove(0f, false);
        }

        public void SnapToGroundImmediate()
        {
            if (body == null || !TryGetGroundY(body.position, out var groundY))
            {
                return;
            }

            var position = body.position;
            position.y = groundY - visualBottomOffset;
            transform.position = position;
            body.position = position;
            StopBody();
        }

        [Inject]
        public void Construct(
            EatResolver eatResolver,
            GrowthSystem growthSystem,
            PlayerProgress progress,
            GameStateController gameState,
            StageRule stageRule,
            DeathEffectService deathEffectService,
            GameEventBus eventBus,
            DinoDataRepository dinoDataRepository,
            PlayerGrowthDataRepository playerGrowthDataRepository,
            CameraReference cameraReference)
        {
            this.eatResolver = eatResolver;
            this.growthSystem = growthSystem;
            this.progress = progress;
            this.gameState = gameState;
            this.stageRule = stageRule;
            this.deathEffectService = deathEffectService;
            this.eventBus = eventBus;
            this.dinoDataRepository = dinoDataRepository;
            this.playerGrowthDataRepository = playerGrowthDataRepository;
            cameraTransform ??= cameraReference?.Transform;
            dependenciesReady = true;
        }

        private void Reset()
        {
            body = GetComponent<Rigidbody>();
            visualRoot = transform;
        }

        private void Awake()
        {
            UseGroundLayerIfAvailable();
            UseDefaultObstacleLayers();

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            ConfigureBody();

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            baseVisualLocalPosition = visualRoot.localPosition;
            ApplyVisualGroundOffset();
            baseVisualScale = visualRoot.localScale;
            CacheVisualBottomOffset();
            CacheObstacleShape();

            if (animatorView == null)
            {
                animatorView = GetComponentInChildren<DinoAnimatorView>();
            }

            if (mouthEffectOrigin == null)
            {
                mouthEffectOrigin = TransformSearchUtility.FindChildByName(visualRoot != null ? visualRoot : transform, "Head_end")
                    ?? TransformSearchUtility.FindChildByName(visualRoot != null ? visualRoot : transform, "Head");
            }

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

        private void UseDefaultObstacleLayers()
        {
            if (obstacleLayers.value != ~0)
            {
                return;
            }

            var mask = Physics.DefaultRaycastLayers;
            var groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                mask &= ~(1 << groundLayer);
            }

            var ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycastLayer >= 0)
            {
                mask &= ~(1 << ignoreRaycastLayer);
            }

            var waterLayer = LayerMask.NameToLayer("Water");
            if (waterLayer >= 0)
            {
                mask &= ~(1 << waterLayer);
            }

            obstacleLayers = mask;
        }

        private void Start()
        {
            if (!EnsureDependenciesReady())
            {
                enabled = false;
                return;
            }

            WarnIfCameraMissing();
            ApplyPlayerData();

            eventBus.PlayerGrowthChanged += OnPlayerGrowthChanged;

            ApplyGrowthVisuals();
            eventBus.PublishPlayerGrowthChanged(new GrowthResult(
                0,
                0,
                progress.Level,
                progress.CurrentExp,
                progress.IsMaxLevel));
        }

        private void Update()
        {
            if (!dependenciesReady || gameState == null)
            {
                rotateInput = Vector2.zero;
                return;
            }

            if (!gameState.IsPlaying)
            {
                rotateInput = Vector2.zero;
                return;
            }

            rotateInput = PlayerInputReader.ReadMoveInput();
            isSprinting = PlayerInputReader.IsSprintPressed();
        }

        private void FixedUpdate()
        {
            if (!dependenciesReady || gameState == null)
            {
                if (body != null)
                {
                    StopBody();
                }

                return;
            }

            if (!gameState.IsPlaying)
            {
                StopBody();
                return;
            }

            if (PlayerMovementUtility.TryGetCameraRelativeDirection(rotateInput, cameraTransform, out var targetDirection))
            {
                var targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
                MoveBody(targetDirection * GetCurrentMoveSpeed());
                animatorView?.SetMove(isSprinting ? 1f : 0.5f, isSprinting);
            }
            else
            {
                MoveBody(Vector3.zero);
                animatorView?.SetMove(0f, false);
            }

            ResolveNearbyEnemyContact();
        }

        private void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.PlayerGrowthChanged -= OnPlayerGrowthChanged;
            }

        }

        private void OnTriggerEnter(Collider other)
        {
            if (!dependenciesReady || gameState == null || !gameState.IsPlaying)
            {
                return;
            }

            var enemy = other.GetComponentInParent<DinoEnemy>();
            if (enemy == null)
            {
                return;
            }

            ResolveEnemyContact(enemy);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!dependenciesReady || gameState == null || !gameState.IsPlaying)
            {
                return;
            }

            var enemy = other.GetComponentInParent<DinoEnemy>();
            if (enemy == null)
            {
                return;
            }

            ResolveEnemyContact(enemy);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!dependenciesReady || gameState == null || !gameState.IsPlaying)
            {
                return;
            }

            var enemy = collision.collider.GetComponentInParent<DinoEnemy>();
            if (enemy == null)
            {
                return;
            }

            ResolveEnemyContact(enemy);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!dependenciesReady || gameState == null || !gameState.IsPlaying)
            {
                return;
            }

            var enemy = collision.collider.GetComponentInParent<DinoEnemy>();
            if (enemy == null)
            {
                return;
            }

            ResolveEnemyContact(enemy);
        }

        private void ResolveEnemyContact(DinoEnemy enemy)
        {
            if (enemy == null || enemy.IsDying || eatResolver == null || progress == null)
            {
                return;
            }

            if (isDead || resolvingEnemy == enemy || Time.time < nextEatContactTime)
            {
                return;
            }

            resolvingEnemy = enemy;
            nextEatContactTime = Time.time + Mathf.Max(0f, eatContactCooldown);
            var result = eatResolver.Resolve(progress.Level, enemy.Level);
            if (result == EatResult.Eat)
            {
                animatorView?.PlayAttack();
                Eat(enemy);
            }
            else
            {
                TriggerGameOver(enemy);
            }

            resolvingEnemy = null;
        }

        private void ResolveNearbyEnemyContact()
        {
            if (!dependenciesReady || gameState == null || !gameState.IsPlaying || isDead)
            {
                return;
            }

            var radius = Mathf.Max(0.1f, enemyContactRadius * Mathf.Max(1f, transform.lossyScale.x));
            var overlaps = Physics.OverlapSphere(transform.position, radius, enemyContactLayers, QueryTriggerInteraction.Collide);
            foreach (var overlap in overlaps)
            {
                var enemy = overlap.GetComponentInParent<DinoEnemy>();
                if (enemy == null || enemy.IsDying)
                {
                    continue;
                }

                var offset = enemy.transform.position - transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude > radius * radius)
                {
                    continue;
                }

                ResolveEnemyContact(enemy);
                return;
            }

            ResolveNearbyRegisteredEnemyContact(radius);
        }

        private void ResolveNearbyRegisteredEnemyContact(float playerRadius)
        {
            var enemies = DinoEnemy.Active;
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                if (enemy == null || enemy.IsDying || !enemy.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var offset = enemy.transform.position - transform.position;
                offset.y = 0f;
                var contactDistance = playerRadius + enemy.GetContactRadius();
                if (offset.sqrMagnitude > contactDistance * contactDistance)
                {
                    continue;
                }

                ResolveEnemyContact(enemy);
                return;
            }
        }

        private void Eat(DinoEnemy enemy)
        {
            var enemyLevel = enemy.Level;
            enemy.Eaten();
            deathEffectService?.SpawnBlood(GetMouthEffectPosition());

            var growthResult = growthSystem.AddEnemyExp(progress, enemyLevel);
            ApplyGrowthVisuals();

            eventBus.PublishEnemyEaten(enemyLevel, growthResult.GainedExp);
            eventBus.PublishPlayerGrowthChanged(growthResult);

            if (stageRule.IsClearLevel(progress.Level))
            {
                gameState.Clear();
                eventBus.PublishGameStateChanged(gameState.State);
            }
        }

        private void OnPlayerGrowthChanged(GrowthResult result)
        {
            ApplyGrowthVisuals();
        }

        private void TriggerGameOver()
        {
            TriggerGameOver(transform.position + Vector3.up * 0.75f);
        }

        private void TriggerGameOver(DinoEnemy attacker)
        {
            if (attacker == null)
            {
                TriggerGameOver();
                return;
            }

            attacker.OnPlayerBitten();
            TriggerGameOver(attacker.GetMouthEffectPosition());
        }

        private void TriggerGameOver(Vector3 bloodEffectPosition)
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            gameState.GameOver();
            rotateInput = Vector2.zero;
            isSprinting = false;
            StopBody();
            animatorView?.SetMove(0f, false);
            deathEffectService?.SpawnBlood(bloodEffectPosition);
            animatorView?.SetDead(true);
            eventBus.PublishGameStateChanged(gameState.State);
        }

        private Vector3 GetMouthEffectPosition()
        {
            if (mouthEffectOrigin != null)
            {
                return mouthEffectOrigin.position;
            }

            return transform.TransformPoint(mouthEffectFallbackOffset);
        }

        private float GetCurrentMoveSpeed()
        {
            return isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
        }

        private bool EnsureDependenciesReady()
        {
            if (dependenciesReady
                && eatResolver != null
                && growthSystem != null
                && progress != null
                && gameState != null
                && stageRule != null
                && eventBus != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(PlayerDinoController)} was not injected by VContainer. Check GameLifetimeScope scene references.", this);
            return false;
        }

        private void ApplyGrowthVisuals()
        {
            if (!applyGrowthScale)
            {
                return;
            }

            ApplyVisualGroundOffset();
            var growthScale = GetGrowthScale(progress.Level);
            visualRoot.localScale = baseVisualScale * growthScale;
            CacheVisualBottomOffset();
            CacheObstacleShape();
        }

        private void MoveBody(Vector3 horizontalVelocity)
        {
            if (body == null)
            {
                return;
            }

            var moveDelta = horizontalVelocity * Time.fixedDeltaTime;
            var hasMoveInput = moveDelta.sqrMagnitude > 0.000001f;
            var resolvedMoveDelta = ResolveObstacleMove(moveDelta);
            if (hasMoveInput && resolvedMoveDelta.sqrMagnitude <= 0.000001f)
            {
                resolvedMoveDelta = moveDelta;
            }

            moveDelta = resolvedMoveDelta;
            var position = body.position + moveDelta;
            if (hasMoveInput)
            {
                position += ResolveObstaclePenetration(position);
            }

            position = PlayerMovementUtility.ClampToBounds(position, useMovementBounds, movementBoundsCenter, movementBoundsSize);
            if (TryGetGroundY(position, out var groundY))
            {
                var targetY = groundY - visualBottomOffset;
                position.y = Mathf.MoveTowards(body.position.y, targetY, maxGroundSnapStep);
            }

            body.MovePosition(position);
            StopBody();
        }

        private Vector3 ResolveObstaclePenetration(Vector3 rootPosition)
        {
            PlayerMovementUtility.GetObstacleCapsule(
                rootPosition,
                obstacleRadius,
                obstacleHeight,
                obstacleCenterOffset,
                out var point1,
                out var point2,
                out var radius);
            var overlaps = Physics.OverlapCapsule(
                point1,
                point2,
                radius,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);

            var correction = Vector3.zero;
            var capsuleCenter = (point1 + point2) * 0.5f;
            foreach (var overlap in overlaps)
            {
                if (PlayerMovementUtility.ShouldIgnoreObstacle(overlap, groundLayers))
                {
                    continue;
                }

                var closestPoint = overlap.ClosestPoint(capsuleCenter);
                var direction = capsuleCenter - closestPoint;
                direction.y = 0f;
                var distance = direction.magnitude;
                if (distance >= radius)
                {
                    continue;
                }

                if (distance <= 0.000001f)
                {
                    continue;
                }

                correction += direction.normalized * (radius - distance + obstacleSkinWidth);
            }

            correction.y = 0f;
            var maxStep = Mathf.Max(0.01f, maxObstacleCorrectionStep);
            if (correction.sqrMagnitude > maxStep * maxStep)
            {
                correction = correction.normalized * maxStep;
            }

            return correction;
        }

        private Vector3 ResolveObstacleMove(Vector3 moveDelta)
        {
            moveDelta.y = 0f;
            if (moveDelta.sqrMagnitude <= 0.000001f)
            {
                return Vector3.zero;
            }

            if (!IsObstacleInMove(moveDelta, out var hit))
            {
                return moveDelta;
            }

            var slide = Vector3.ProjectOnPlane(moveDelta, hit.normal);
            slide.y = 0f;
            if (slide.sqrMagnitude <= 0.000001f || IsObstacleInMove(slide, out _))
            {
                return Vector3.zero;
            }

            return slide;
        }

        private bool IsObstacleInMove(Vector3 moveDelta, out RaycastHit hit)
        {
            var distance = moveDelta.magnitude;
            if (distance <= 0.0001f)
            {
                hit = default;
                return false;
            }

            var direction = moveDelta / distance;
            PlayerMovementUtility.GetObstacleCapsule(
                body.position,
                obstacleRadius,
                obstacleHeight,
                obstacleCenterOffset,
                out var point1,
                out var point2,
                out var radius);
            var castDistance = distance + obstacleSkinWidth;
            var hits = Physics.CapsuleCastAll(
                point1,
                point2,
                radius,
                direction,
                castDistance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var candidate in hits)
            {
                if (PlayerMovementUtility.ShouldIgnoreObstacle(candidate.collider, groundLayers))
                {
                    continue;
                }

                hit = candidate;
                return true;
            }

            hit = default;
            return false;
        }

        private void ConfigureBody()
        {
            if (body == null)
            {
                return;
            }

            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            StopBody();
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
                if (hit.collider.GetComponentInParent<PlayerDinoController>() != null)
                {
                    continue;
                }

                if (hit.collider.GetComponentInParent<DinoEnemy>() != null)
                {
                    continue;
                }

                if (PlayerMovementUtility.IsWaterCollider(hit.collider))
                {
                    continue;
                }

                groundY = hit.point.y + groundOffset;
                return true;
            }

            return false;
        }

        private void CacheVisualBottomOffset()
        {
            ApplyVisualGroundOffset();
            var bounds = CalculateWorldVisualBounds();
            visualBottomOffset = bounds.HasValue
                ? bounds.Value.min.y - transform.position.y
                : 0f;
        }

        private void ApplyVisualGroundOffset()
        {
            if (visualRoot == null || visualRoot == transform)
            {
                return;
            }

            var localPosition = baseVisualLocalPosition;
            localPosition.y += visualGroundOffset;
            visualRoot.localPosition = localPosition;
        }

        private void CacheObstacleShape()
        {
            var bounds = CalculateWorldVisualBounds();
            if (!bounds.HasValue)
            {
                obstacleRadius = minObstacleRadius;
                obstacleHeight = minObstacleHeight;
                obstacleCenterOffset = Vector3.up * (obstacleHeight * 0.5f);
                return;
            }

            var value = bounds.Value;
            obstacleRadius = Mathf.Max(minObstacleRadius, Mathf.Min(value.size.x, value.size.z) * 0.28f);
            obstacleHeight = Mathf.Max(minObstacleHeight, value.size.y * 0.85f);
            var center = value.center;
            center.y = value.min.y + obstacleHeight * 0.5f;
            obstacleCenterOffset = center - transform.position;
        }

        private Bounds? CalculateWorldVisualBounds()
        {
            return RendererBoundsUtility.TryCalculateVisibleBounds(transform, out var bounds) ? bounds : null;
        }

        private void ApplyPlayerData()
        {
            if (dinoDataRepository == null || !dinoDataRepository.TryGetById(playerDataId, out var playerData))
            {
                return;
            }

            if (playerData.speed > 0f)
            {
                moveSpeed = playerData.speed;
            }

            if (useDataSize && playerData.size > 0f)
            {
                baseVisualScale = Vector3.one * playerData.size;
            }
        }

        private float GetGrowthScale(int level)
        {
            if (playerGrowthDataRepository != null
                && playerGrowthDataRepository.TryGetByLevel(level, out var growthData)
                && growthData.scaleMultiplier > 0f)
            {
                return growthData.scaleMultiplier;
            }

            return Mathf.Lerp(1f, 4.25f, Mathf.InverseLerp(1f, 20f, level));
        }

        private void WarnIfCameraMissing()
        {
            if (cameraTransform != null)
            {
                return;
            }

            Debug.LogWarning($"{nameof(PlayerDinoController)} needs an explicit camera transform reference.", this);
        }
    }
}
