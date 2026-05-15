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
        [SerializeField] private float turnSpeed = 540f;
        [SerializeField] private Rigidbody body;
        [SerializeField] private Transform visualRoot;

        private EatResolver eatResolver;
        private GrowthSystem growthSystem;
        private PlayerProgress progress;
        private GameStateController gameState;
        private StageRule stageRule;
        private GameEventBus eventBus;
        private Vector3 moveInput;

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
        }

        private void Start()
        {
            gameState.StartGame();
            eventBus.PublishGameStateChanged(gameState.State);
            ApplyGrowthVisuals();
        }

        private void Update()
        {
            if (!gameState.IsPlaying)
            {
                moveInput = Vector3.zero;
                return;
            }

            moveInput = ReadMoveInput();
            moveInput = Vector3.ClampMagnitude(moveInput, 1f);
        }

        private void FixedUpdate()
        {
            if (!gameState.IsPlaying)
            {
                body.linearVelocity = Vector3.zero;
                return;
            }

            var speed = GetMoveSpeed();
            var velocity = moveInput * speed;
            body.linearVelocity = new Vector3(velocity.x, body.linearVelocity.y, velocity.z);

            if (moveInput.sqrMagnitude > 0.001f)
            {
                var targetRotation = Quaternion.LookRotation(moveInput, Vector3.up);
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
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

        private void TriggerGameOver()
        {
            gameState.GameOver();
            body.linearVelocity = Vector3.zero;
            eventBus.PublishGameStateChanged(gameState.State);
        }

        private float GetMoveSpeed()
        {
            return Mathf.Lerp(5.2f, 4.25f, Mathf.InverseLerp(1f, 20f, progress.Level));
        }

        private static Vector3 ReadMoveInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector3.zero;
            }

            var x = 0f;
            var z = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                x -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                x += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                z -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                z += 1f;
            }

            return new Vector3(x, 0f, z);
        }

        private void ApplyGrowthVisuals()
        {
            var scale = Mathf.Lerp(1f, 4.25f, Mathf.InverseLerp(1f, 20f, progress.Level));
            visualRoot.localScale = Vector3.one * scale;
        }
    }
}
