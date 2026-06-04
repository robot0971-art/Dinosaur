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
        [SerializeField] private float damageContactCooldown = 1f;
        [SerializeField] private GameHudHeartUI heartUI;
        [SerializeField] private AudioClip hitSoundClip;
        [SerializeField] private AudioSource hitSoundSource;
        [SerializeField, Range(0f, 1f)] private float hitSoundVolume = 1f;
        [SerializeField] private AudioClip sprintStepClip;
        [SerializeField] private AudioSource sprintStepSource;
        [SerializeField, Range(0f, 1f)] private float sprintStepVolume = 0.65f;
        [SerializeField, Range(0.1f, 3f)] private float sprintStepPitch = 1f;
        [SerializeField, Min(0f)] private float sprintStepLoopDelay = 0.2f;
        [SerializeField] private AudioClip walkStepClip;
        [SerializeField] private AudioSource walkStepSource;
        [SerializeField, Range(0f, 1f)] private float walkStepVolume = 0.8f;
        [SerializeField, Range(0.1f, 3f)] private float walkStepPitch = 1f;
        [SerializeField, Min(0f)] private float walkStepLoopDelay = 0.2f;
        [SerializeField] private bool useMovementBounds;
        [SerializeField] private Vector3 movementBoundsCenter;
        [SerializeField] private Vector2 movementBoundsSize = new(80f, 80f);
        private EatResolver eatResolver;
        private GrowthSystem growthSystem;
        private PlayerProgress progress;
        private GameStateController gameState;
        private StageRule stageRule;
        private DeathEffectService deathEffectService;
        private EatingSoundService eatingSoundService;
        private GameEventBus eventBus;
        private DinoDataRepository dinoDataRepository;
        private PlayerGrowthDataRepository playerGrowthDataRepository;
        private Vector2 rotateInput;
        private bool isSprinting;
        private bool isDead;
        private bool dependenciesReady;
        private DinoEnemy resolvingEnemy;
        private float nextEatContactTime;
        private readonly PlayerEnemyContactScanner enemyContactScanner = new(32);
        private PlayerHitHandler hitHandler;
        private PlayerEatHandler eatHandler;
        private PlayerMovementMotor movementMotor;
        private PlayerStepSoundController stepSoundController;
        private PlayerGrowthVisualController growthVisualController;
        private PlayerDeathHandler deathHandler;

        public int Level => progress?.Level ?? 1;

        public void ConfigureHeartUI(GameHudHeartUI ui)
        {
            heartUI = ui;
        }

        public bool TryAddHeart()
        {
            if (heartUI == null || heartUI.GetCurrentHearts() >= heartUI.GetMaxHearts())
            {
                return false;
            }

            heartUI.AddHeart();
            return true;
        }

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
                movementMotor?.StopBody();
            }

            rotateInput = Vector2.zero;
            isSprinting = false;
            animatorView?.SetMove(0f, false);
            stepSoundController?.Stop();
        }

        public void SnapToGroundImmediate()
        {
            if (movementMotor == null || !movementMotor.TrySnapToGround(out _))
            {
                return;
            }
        }

        [Inject]
        public void Construct(
            EatResolver eatResolver,
            GrowthSystem growthSystem,
            PlayerProgress progress,
            GameStateController gameState,
            StageRule stageRule,
            DeathEffectService deathEffectService,
            EatingSoundService eatingSoundService,
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
            this.eatingSoundService = eatingSoundService;
            this.eventBus = eventBus;
            this.dinoDataRepository = dinoDataRepository;
            this.playerGrowthDataRepository = playerGrowthDataRepository;
            cameraTransform ??= cameraReference?.Transform;
            hitHandler = new PlayerHitHandler(
                gameObject,
                deathEffectService,
                hitSoundClip,
                hitSoundSource,
                hitSoundVolume);
            eatHandler = new PlayerEatHandler(
                growthSystem,
                progress,
                stageRule,
                gameState,
                deathEffectService,
                eatingSoundService,
                eventBus);
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

            movementMotor = new PlayerMovementMotor(body, transform);
            ConfigureMovementMotor();
            movementMotor.ConfigureBody();
            stepSoundController = new PlayerStepSoundController(
                gameObject,
                sprintStepClip,
                sprintStepSource,
                sprintStepVolume,
                sprintStepPitch,
                sprintStepLoopDelay,
                walkStepClip,
                walkStepSource,
                walkStepVolume,
                walkStepPitch,
                walkStepLoopDelay);
            stepSoundController.Configure();

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            growthVisualController = new PlayerGrowthVisualController(
                transform,
                visualRoot,
                playerGrowthDataRepository,
                movementMotor,
                applyGrowthScale,
                visualGroundOffset,
                visualRoot.localScale,
                visualRoot.localPosition);
            growthVisualController.ApplyVisualGroundOffset();
            movementMotor.CacheVisualBottomOffset();
            movementMotor.CacheObstacleShape();

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
            ConfigureDeathHandler();
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
                stepSoundController?.Stop();
                return;
            }

            if (!gameState.IsPlaying)
            {
                rotateInput = Vector2.zero;
                stepSoundController?.Stop();
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
                    movementMotor?.StopBody();
                }

                stepSoundController?.Stop();
                return;
            }

            if (!gameState.IsPlaying)
            {
                movementMotor?.StopBody();
                stepSoundController?.Stop();
                return;
            }

            if (PlayerMovementUtility.TryGetCameraRelativeDirection(rotateInput, cameraTransform, out var targetDirection))
            {
                var targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
                MoveBody(targetDirection * GetCurrentMoveSpeed());
                animatorView?.SetMove(isSprinting ? 1f : 0.5f, isSprinting);
                stepSoundController?.UpdateLoops(true, isSprinting);
            }
            else
            {
                MoveBody(Vector3.zero);
                animatorView?.SetMove(0f, false);
                stepSoundController?.Stop();
            }

            ResolveNearbyEnemyContact();
        }

        private void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.PlayerGrowthChanged -= OnPlayerGrowthChanged;
            }

            stepSoundController?.Stop();
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
                nextEatContactTime = Time.time + Mathf.Max(0f, damageContactCooldown);
                TakeHit(enemy);
            }

            resolvingEnemy = null;
        }

        private void ResolveNearbyEnemyContact()
        {
            if (!dependenciesReady || gameState == null || !gameState.IsPlaying || isDead)
            {
                return;
            }

            if (enemyContactScanner.TryFindContact(transform, enemyContactRadius, enemyContactLayers, out var enemy))
            {
                ResolveEnemyContact(enemy);
            }
        }

        private void Eat(DinoEnemy enemy)
        {
            eatHandler?.Eat(enemy, GetMouthEffectPosition(), OnEnemyEatenGrowthChanged);
        }

        private void OnEnemyEatenGrowthChanged(GrowthResult result)
        {
            ApplyGrowthVisuals();
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

        private void TakeHit(DinoEnemy attacker)
        {
            hitHandler?.TakeHit(attacker, heartUI, GetHitEffectPosition(), TriggerGameOver);
        }

        private void TriggerGameOver(Vector3 bloodEffectPosition)
        {
            isDead = deathHandler?.TriggerGameOver(isDead, bloodEffectPosition, ClearMovementInput) ?? isDead;
        }

        private void ClearMovementInput()
        {
            rotateInput = Vector2.zero;
            isSprinting = false;
        }

        private Vector3 GetMouthEffectPosition()
        {
            if (mouthEffectOrigin != null)
            {
                return mouthEffectOrigin.position;
            }

            return transform.TransformPoint(mouthEffectFallbackOffset);
        }

        private Vector3 GetHitEffectPosition()
        {
            return transform.position + Vector3.up * 0.75f;
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
            growthVisualController?.ApplyGrowthVisuals(progress);
        }

        private void MoveBody(Vector3 horizontalVelocity)
        {
            movementMotor?.MoveBody(horizontalVelocity, useMovementBounds, movementBoundsCenter, movementBoundsSize);
        }

        private void ConfigureMovementMotor()
        {
            movementMotor?.Configure(
                groundLayers,
                groundRaycastHeight,
                groundRaycastDistance,
                groundOffset,
                maxGroundSnapStep,
                obstacleLayers,
                obstacleSkinWidth,
                maxObstacleCorrectionStep,
                minObstacleRadius,
                minObstacleHeight);
        }

        private void ConfigureDeathHandler()
        {
            deathHandler = new PlayerDeathHandler(
                gameState,
                deathEffectService,
                eventBus,
                movementMotor,
                stepSoundController,
                animatorView);
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
                growthVisualController?.SetBaseVisualScale(Vector3.one * playerData.size);
            }
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
