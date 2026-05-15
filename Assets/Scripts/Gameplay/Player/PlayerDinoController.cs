using DinoGrow.Core.Combat;
using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;
using DinoGrow.Gameplay.Enemy;
using DinoGrow.Infrastructure.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace DinoGrow.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerDinoController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float turnSpeed = 540f;
        [SerializeField] private Rigidbody body;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private TextMesh levelText;
        [SerializeField] private Vector3 levelTextOffset = new Vector3(0f, 2.4f, 0f);
        [SerializeField] private float levelTextCharacterSize = 0.22f;
        [SerializeField] private int levelTextFontSize = 72;
        [SerializeField] private Color levelTextColor = Color.white;

        private EatResolver eatResolver;
        private GrowthSystem growthSystem;
        private PlayerProgress progress;
        private GameStateController gameState;
        private StageRule stageRule;
        private GameEventBus eventBus;
        private Vector2 rotateInput;
        private Transform levelTextTransform;
        private bool createdLevelText;

        public int Level => progress?.Level ?? 1;

        [Inject]
        public void Construct(
            EatResolver eatResolver,
            GrowthSystem growthSystem,
            PlayerProgress progress,
            GameStateController gameState,
            StageRule stageRule,
            GameEventBus eventBus)
        {
            this.eatResolver = eatResolver;
            this.growthSystem = growthSystem;
            this.progress = progress;
            this.gameState = gameState;
            this.stageRule = stageRule;
            this.eventBus = eventBus;
        }

        private void Reset()
        {
            body = GetComponent<Rigidbody>();
            visualRoot = transform;
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (cameraTransform == null && UnityEngine.Camera.main != null)
            {
                cameraTransform = UnityEngine.Camera.main.transform;
            }
        }

        private void Start()
        {
            EnsureLevelText();
            RefreshLevelText();

            eventBus.PlayerGrowthChanged += OnPlayerGrowthChanged;

            gameState.StartGame();
            eventBus.PublishGameStateChanged(gameState.State);
            ApplyGrowthVisuals();
        }

        private void Update()
        {
            if (!gameState.IsPlaying)
            {
                rotateInput = Vector2.zero;
                return;
            }

            rotateInput = ReadRotateInput();
        }

        private void LateUpdate()
        {
            UpdateLevelTextTransform();
        }

        private void FixedUpdate()
        {
            if (!gameState.IsPlaying)
            {
                body.linearVelocity = Vector3.zero;
                return;
            }

            if (TryGetCameraRelativeDirection(rotateInput, out var targetDirection))
            {
                // 캐릭터 회전: 이동 방향을 향하도록
                var targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));

                // 캐릭터 이동: 카메라 기준 방향으로 이동
                body.linearVelocity = new Vector3(
                    targetDirection.x * moveSpeed,
                    body.linearVelocity.y,
                    targetDirection.z * moveSpeed
                );
            }
            else
            {
                // 입력 없으면 수평 이동 정지
                body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
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
            if (!gameState.IsPlaying || !other.TryGetComponent(out DinoEnemy enemy))
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

        private void Eat(DinoEnemy enemy)
        {
            var enemyLevel = enemy.Level;
            enemy.Eaten();

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
            gameState.GameOver();
            body.linearVelocity = Vector3.zero;
            eventBus.PublishGameStateChanged(gameState.State);
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
            UpdateLevelTextTransform();
        }

        private void RefreshLevelText()
        {
            if (levelText == null || progress == null)
            {
                return;
            }

            levelText.text = progress.IsMaxLevel
                ? $"Lv. {progress.Level}\nEXP MAX"
                : $"Lv. {progress.Level}\nEXP {progress.CurrentExp}/{progress.ExpToLevelUp}";
        }

        private void UpdateLevelTextTransform()
        {
            if (levelTextTransform == null)
            {
                return;
            }

            if (cameraTransform == null && UnityEngine.Camera.main != null)
            {
                cameraTransform = UnityEngine.Camera.main.transform;
            }

            levelTextTransform.position = transform.position + levelTextOffset;

            if (cameraTransform != null)
            {
                var lookDirection = levelTextTransform.position - cameraTransform.position;
                lookDirection.y = 0f;

                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    levelTextTransform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
                }
            }
        }

        private void ApplyGrowthVisuals()
        {
            var scale = Mathf.Lerp(1f, 4.25f, Mathf.InverseLerp(1f, 20f, progress.Level));
            visualRoot.localScale = Vector3.one * scale;
        }
    }
}
