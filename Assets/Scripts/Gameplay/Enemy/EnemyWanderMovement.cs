using UnityEngine;
using DinoGrow.Gameplay.Animation;
using DinoGrow.Gameplay.Player;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class EnemyWanderMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float turnSpeed = 420f;
        [SerializeField] private float directionChangeInterval = 1.6f;
        [SerializeField] private float fleeDetectDistance = 18f;
        [SerializeField] private float fleeSpeedMultiplier = 1.65f;
        [SerializeField] private DinoAnimatorView animatorView;
        [SerializeField] private Vector3 areaCenter;
        [SerializeField] private Vector2 areaSize = new(80f, 80f);

        private DinoEnemy enemy;
        private Transform player;
        private PlayerDinoController playerController;
        private Vector3 moveDirection;
        private float nextDirectionTime;

        public void Configure(Vector3 center, Vector2 size, float speed, Transform playerTransform)
        {
            areaCenter = center;
            areaSize = size;
            moveSpeed = speed;
            SetPlayer(playerTransform);
            PickNewDirection();
        }

        private void Awake()
        {
            enemy = GetComponent<DinoEnemy>();
            if (animatorView == null)
            {
                animatorView = GetComponentInChildren<DinoAnimatorView>();
            }
        }

        private void Start()
        {
            PickNewDirection();
        }

        private void Update()
        {
            if (TryGetFleeDirection(out var fleeDirection))
            {
                Move(fleeDirection, moveSpeed * fleeSpeedMultiplier);
                animatorView?.SetMove(1f, true);
                return;
            }

            if (Time.time >= nextDirectionTime || IsNearAreaEdge())
            {
                PickNewDirection();
            }

            Move(moveDirection, moveSpeed);
            animatorView?.SetMove(0.5f, false);
        }

        private void SetPlayer(Transform playerTransform)
        {
            player = playerTransform;
            playerController = player != null ? player.GetComponent<PlayerDinoController>() : null;
        }

        private bool TryGetFleeDirection(out Vector3 fleeDirection)
        {
            fleeDirection = Vector3.zero;
            if (enemy == null || player == null || playerController == null)
            {
                return false;
            }

            if (enemy.Level >= playerController.Level)
            {
                return false;
            }

            var awayFromPlayer = transform.position - player.position;
            awayFromPlayer.y = 0f;
            if (awayFromPlayer.sqrMagnitude > fleeDetectDistance * fleeDetectDistance)
            {
                return false;
            }

            if (awayFromPlayer.sqrMagnitude <= 0.001f)
            {
                awayFromPlayer = Random.onUnitSphere;
                awayFromPlayer.y = 0f;
            }

            fleeDirection = awayFromPlayer.normalized;
            moveDirection = fleeDirection;
            nextDirectionTime = Time.time + directionChangeInterval;
            return true;
        }

        private void Move(Vector3 direction, float speed)
        {
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            transform.position += direction * (speed * Time.deltaTime);
            ClampToArea();
        }

        private void PickNewDirection()
        {
            var random = Random.insideUnitCircle.normalized;
            moveDirection = new Vector3(random.x, 0f, random.y);
            nextDirectionTime = Time.time + Random.Range(directionChangeInterval * 0.7f, directionChangeInterval * 1.3f);
        }

        private bool IsNearAreaEdge()
        {
            var halfSize = areaSize * 0.5f;
            var local = transform.position - areaCenter;
            return Mathf.Abs(local.x) > halfSize.x * 0.9f || Mathf.Abs(local.z) > halfSize.y * 0.9f;
        }

        private void ClampToArea()
        {
            var halfSize = areaSize * 0.5f;
            var position = transform.position;
            position.x = Mathf.Clamp(position.x, areaCenter.x - halfSize.x, areaCenter.x + halfSize.x);
            position.z = Mathf.Clamp(position.z, areaCenter.z - halfSize.y, areaCenter.z + halfSize.y);
            transform.position = position;
        }
    }
}
