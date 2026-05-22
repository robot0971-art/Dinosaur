using DinoGrow.Core.Combat;
using DinoGrow.Core.Data;
using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;
using DinoGrow.Gameplay.Animation;
using DinoGrow.Gameplay.Enemy;
using DinoGrow.Infrastructure.Data;
using DinoGrow.Infrastructure.DI;
using DinoGrow.Infrastructure.Events;
using UnityEngine;
using UnityEngine.InputSystem;
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
        [SerializeField] private TextMesh levelText;
        [SerializeField] private Vector3 levelTextOffset = new Vector3(0f, 1.55f, 0f);
        [SerializeField] private float levelTextCharacterSize = 0.045f;
        [SerializeField] private int levelTextFontSize = 36;
        [SerializeField] private Color levelTextColor = Color.white;

        private EatResolver eatResolver;
        private GrowthSystem growthSystem;
        private PlayerProgress progress;
        private GameStateController gameState;
        private StageRule stageRule;
        private DeathEffectService deathEffectService;
        private GameEventBus eventBus;
        private HeartsSystem heartsSystem;
        private PlayerDataRepository playerDataRepository;
        private PlayerGrowthDataRepository playerGrowthDataRepository;
        private Vector2 rotateInput;
        private bool isSprinting;
        private Transform levelTextTransform;
        private bool createdLevelText;
        private Vector3 baseVisualScale = Vector3.one;
        private Vector3 baseVisualLocalPosition;
        private bool isDead;
        private bool dependenciesReady;
        private float visualBottomOffset;
        private float obstacleRadius = 0.45f;
        private float obstacleHeight = 1.8f;
        private Vector3 obstacleCenterOffset = new Vector3(0f, 0.9f, 0f);
        private Collider obstacleProbeCollider;
        private CapsuleCollider obstacleProbeCapsule;
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
            HeartsSystem heartsSystem,
            PlayerDataRepository playerDataRepository,
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
            this.heartsSystem = heartsSystem;
            this.playerDataRepository = playerDataRepository;
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
            CacheObstacleProbeCollider();

            if (animatorView == null)
            {
                animatorView = GetComponentInChildren<DinoAnimatorView>();
            }

            if (mouthEffectOrigin == null)
            {
                mouthEffectOrigin = FindChildByName(visualRoot != null ? visualRoot : transform, "Head_end")
                    ?? FindChildByName(visualRoot != null ? visualRoot : transform, "Head");
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
            EnsureLevelText();
            RefreshLevelText();

            eventBus.PlayerGrowthChanged += OnPlayerGrowthChanged;

            gameState.StartGame();
            eventBus.PublishGameStateChanged(gameState.State);
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

            rotateInput = ReadRotateInput();
            isSprinting = IsSprintPressed();
        }

        private void LateUpdate()
        {
            UpdateLevelTextTransform();
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

            if (TryGetCameraRelativeDirection(rotateInput, out var targetDirection))
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

            if (createdLevelText && levelText != null)
            {
                Destroy(levelText.gameObject);
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
            else if (heartsSystem != null && heartsSystem.IsAlive)
            {
                heartsSystem.LoseLife();
                animatorView?.SetHit(true);
                deathEffectService?.SpawnBlood(GetMouthEffectPosition());
                eventBus.PublishEnemyEaten(enemy.Level, 0);

                if (heartsSystem.IsDead)
                {
                    TriggerGameOver(enemy);
                }
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
            RefreshLevelText();
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

        private static Transform FindChildByName(Transform root, string targetName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == targetName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var result = FindChildByName(root.GetChild(i), targetName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static Vector2 ReadRotateInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            var input = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                input.x -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                input.x += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                input.y -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                input.y += 1f;
            }

            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private static bool IsSprintPressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        }

        private float GetCurrentMoveSpeed()
        {
            return isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
        }

        private bool TryGetCameraRelativeDirection(Vector2 input, out Vector3 direction)
        {
            direction = Vector3.zero;
            if (input.sqrMagnitude <= 0.001f || cameraTransform == null)
            {
                return false;
            }

            var cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            var cameraRight = cameraTransform.right;
            cameraRight.y = 0f;

            if (cameraForward.sqrMagnitude <= 0.001f || cameraRight.sqrMagnitude <= 0.001f)
            {
                return false;
            }

            direction = cameraForward.normalized * input.y + cameraRight.normalized * input.x;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return false;
            }

            direction.Normalize();
            return true;
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

        private void EnsureLevelText()
        {
            if (levelText == null)
            {
                var labelObject = new GameObject("PlayerLevelText");
                levelText = labelObject.AddComponent<TextMesh>();
                createdLevelText = true;
            }

            levelTextTransform = levelText.transform;
            levelText.anchor = TextAnchor.MiddleCenter;
            levelText.alignment = TextAlignment.Center;
            levelText.characterSize = levelTextCharacterSize;
            levelText.fontSize = levelTextFontSize;
            levelText.color = levelTextColor;
            ConfigureLevelTextMaterial(levelText);
            UpdateLevelTextTransform();
        }

        private static void ConfigureLevelTextMaterial(TextMesh targetText)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            if (font == null)
            {
                return;
            }

            targetText.font = font;

            if (targetText.TryGetComponent<MeshRenderer>(out var textRenderer))
            {
                textRenderer.sharedMaterial = font.material;
            }
        }

        private void RefreshLevelText()
        {
            if (levelText == null || progress == null)
            {
                return;
            }

            levelText.text = progress.IsMaxLevel ? $"Lv. {progress.Level} MAX" : $"Lv. {progress.Level}";
        }

        private void UpdateLevelTextTransform()
        {
            if (levelTextTransform == null)
            {
                return;
            }

            levelTextTransform.position = transform.position + levelTextOffset;

            if (cameraTransform != null)
            {
                var lookDirection = levelTextTransform.position - cameraTransform.position;

                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    levelTextTransform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
                }
            }
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

            position = ClampToMovementBounds(position);
            if (TryGetGroundY(position, out var groundY))
            {
                var targetY = groundY - visualBottomOffset;
                position.y = Mathf.MoveTowards(body.position.y, targetY, maxGroundSnapStep);
            }

            body.MovePosition(position);
            StopBody();
        }

        private Vector3 ClampToMovementBounds(Vector3 position)
        {
            if (!useMovementBounds)
            {
                return position;
            }

            var halfSize = movementBoundsSize * 0.5f;
            position.x = Mathf.Clamp(
                position.x,
                movementBoundsCenter.x - halfSize.x,
                movementBoundsCenter.x + halfSize.x);
            position.z = Mathf.Clamp(
                position.z,
                movementBoundsCenter.z - halfSize.y,
                movementBoundsCenter.z + halfSize.y);
            return position;
        }

        private Vector3 ResolveObstaclePenetration(Vector3 rootPosition)
        {
            GetObstacleCapsule(rootPosition, out var point1, out var point2, out var radius);
            var overlaps = Physics.OverlapCapsule(
                point1,
                point2,
                radius,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);

            var correction = Vector3.zero;
            foreach (var overlap in overlaps)
            {
                if (ShouldIgnoreObstacle(overlap))
                {
                    continue;
                }

                if (obstacleProbeCollider == null
                    || !Physics.ComputePenetration(
                        obstacleProbeCollider,
                        rootPosition,
                        transform.rotation,
                        overlap,
                        overlap.transform.position,
                        overlap.transform.rotation,
                        out var direction,
                        out var distance))
                {
                    continue;
                }

                direction.y = 0f;
                if (direction.sqrMagnitude <= 0.000001f)
                {
                    continue;
                }

                correction += direction.normalized * (distance + obstacleSkinWidth);
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
            GetObstacleCapsule(body.position, out var point1, out var point2, out var radius);
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
                if (ShouldIgnoreObstacle(candidate.collider))
                {
                    continue;
                }

                hit = candidate;
                return true;
            }

            hit = default;
            return false;
        }

        private void GetObstacleCapsule(Vector3 rootPosition, out Vector3 point1, out Vector3 point2, out float radius)
        {
            radius = Mathf.Max(0.05f, obstacleRadius);
            var height = Mathf.Max(obstacleHeight, radius * 2f);
            var center = rootPosition + obstacleCenterOffset;
            if (obstacleProbeCapsule != null)
            {
                radius = Mathf.Max(0.05f, obstacleProbeCapsule.radius * GetMaxHorizontalScale(obstacleProbeCapsule.transform));
                var capsuleHeight = Mathf.Max(obstacleProbeCapsule.height * Mathf.Abs(obstacleProbeCapsule.transform.lossyScale.y), radius * 2f);
                center = rootPosition + transform.TransformVector(obstacleProbeCapsule.center);
                var capsuleHalfSegment = Mathf.Max(0f, (capsuleHeight * 0.5f) - radius);
                point1 = center + Vector3.up * capsuleHalfSegment;
                point2 = center - Vector3.up * capsuleHalfSegment;
                return;
            }

            var halfSegment = Mathf.Max(0f, (height * 0.5f) - radius);
            point1 = center + Vector3.up * halfSegment;
            point2 = center - Vector3.up * halfSegment;
        }

        private bool ShouldIgnoreObstacle(Collider targetCollider)
        {
            if (targetCollider == null || targetCollider.isTrigger)
            {
                return true;
            }

            if (targetCollider.GetComponentInParent<PlayerDinoController>() != null)
            {
                return true;
            }

            if (targetCollider.GetComponentInParent<DinoEnemy>() != null)
            {
                return true;
            }

            return IsWaterCollider(targetCollider);
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

                if (IsWaterCollider(hit.collider))
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

        private void CacheObstacleProbeCollider()
        {
            var colliders = GetComponents<Collider>();
            foreach (var targetCollider in colliders)
            {
                if (targetCollider is CapsuleCollider capsule && !capsule.isTrigger)
                {
                    obstacleProbeCollider = capsule;
                    obstacleProbeCapsule = capsule;
                    return;
                }
            }

            foreach (var targetCollider in colliders)
            {
                if (targetCollider != null && !targetCollider.isTrigger)
                {
                    obstacleProbeCollider = targetCollider;
                    obstacleProbeCapsule = targetCollider as CapsuleCollider;
                    return;
                }
            }

            obstacleProbeCollider = null;
            obstacleProbeCapsule = null;
        }

        private static float GetMaxHorizontalScale(Transform target)
        {
            var scale = target.lossyScale;
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
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

        private void ApplyPlayerData()
        {
            if (playerDataRepository == null || !playerDataRepository.TryGetById(playerDataId, out var playerData))
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
