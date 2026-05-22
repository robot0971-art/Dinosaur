using System.Collections.Generic;
using DinoGrow.Core.Combat;
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
        [SerializeField] private float maxGroundSnapStep = 0.18f;
        [SerializeField] private Rigidbody body;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private DinoAnimatorView animatorView;
        [SerializeField] private Transform mouthEffectOrigin;
        [SerializeField] private Vector3 mouthEffectFallbackOffset = new Vector3(0f, 1.05f, 0.75f);
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
        private DinoDataRepository dinoDataRepository;
        private PlayerGrowthDataRepository playerGrowthDataRepository;
        private Vector2 rotateInput;
        private bool isSprinting;
        private Transform levelTextTransform;
        private bool createdLevelText;
        private Vector3 baseVisualScale = Vector3.one;
        private bool isDead;
        private bool dependenciesReady;
        private float visualBottomOffset;
        private readonly List<DinoEnemy> attackTargets = new List<DinoEnemy>();

        public int Level => progress?.Level ?? 1;

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

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            body.interpolation = RigidbodyInterpolation.Interpolate;

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            baseVisualScale = visualRoot.localScale;
            CacheVisualBottomOffset();

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

            if (IsAttackPressed())
            {
                animatorView?.PlayAttack();
                TryAttackTarget();
            }
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
                    body.linearVelocity = Vector3.zero;
                }

                return;
            }

            if (!gameState.IsPlaying)
            {
                body.linearVelocity = Vector3.zero;
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

            if (!other.TryGetComponent(out DinoEnemy enemy))
            {
                return;
            }

            if (!attackTargets.Contains(enemy))
            {
                attackTargets.Add(enemy);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out DinoEnemy enemy))
            {
                return;
            }

            attackTargets.Remove(enemy);
        }

        private void TryAttackTarget()
        {
            if (!dependenciesReady || eatResolver == null || progress == null)
            {
                return;
            }

            var enemy = GetClosestAttackTarget();
            if (enemy == null)
            {
                return;
            }

            var result = eatResolver.Resolve(progress.Level, enemy.Level);
            if (result == EatResult.Eat)
            {
                Eat(enemy);
            }
            else
            {
                TriggerGameOver();
            }
        }

        private DinoEnemy GetClosestAttackTarget()
        {
            DinoEnemy closest = null;
            var closestDistanceSqr = float.PositiveInfinity;

            for (var i = attackTargets.Count - 1; i >= 0; i--)
            {
                var enemy = attackTargets[i];
                if (enemy == null || enemy.IsDying || !enemy.gameObject.activeInHierarchy)
                {
                    attackTargets.RemoveAt(i);
                    continue;
                }

                var offset = enemy.transform.position - transform.position;
                var distanceSqr = offset.sqrMagnitude;
                if (distanceSqr >= closestDistanceSqr)
                {
                    continue;
                }

                closest = enemy;
                closestDistanceSqr = distanceSqr;
            }

            return closest;
        }

        private void Eat(DinoEnemy enemy)
        {
            attackTargets.Remove(enemy);
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
            if (isDead)
            {
                return;
            }

            isDead = true;
            gameState.GameOver();
            body.linearVelocity = Vector3.zero;
            deathEffectService?.SpawnBlood(transform.position + Vector3.up * 0.75f);
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

        private static bool IsAttackPressed()
        {
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
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

            var growthScale = GetGrowthScale(progress.Level);
            visualRoot.localScale = baseVisualScale * growthScale;
            CacheVisualBottomOffset();
        }

        private void MoveBody(Vector3 horizontalVelocity)
        {
            if (body == null)
            {
                return;
            }

            var position = body.position + horizontalVelocity * Time.fixedDeltaTime;

            if (TryGetGroundY(position, out var groundY))
            {
                var targetY = groundY - visualBottomOffset;
                position.y = Mathf.MoveTowards(body.position.y, targetY, maxGroundSnapStep);
            }

            body.MovePosition(position);
            body.linearVelocity = Vector3.zero;
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
