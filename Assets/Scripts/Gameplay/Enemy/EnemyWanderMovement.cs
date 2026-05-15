using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class EnemyWanderMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 1.4f;
        [SerializeField] private float turnSpeed = 240f;
        [SerializeField] private float directionChangeInterval = 2.5f;
        [SerializeField] private Vector3 areaCenter;
        [SerializeField] private Vector2 areaSize = new(80f, 80f);

        private Vector3 moveDirection;
        private float nextDirectionTime;

        public void Configure(Vector3 center, Vector2 size, float speed)
        {
            areaCenter = center;
            areaSize = size;
            moveSpeed = speed;
            PickNewDirection();
        }

        private void Start()
        {
            PickNewDirection();
        }

        private void Update()
        {
            if (Time.time >= nextDirectionTime || IsNearAreaEdge())
            {
                PickNewDirection();
            }

            var targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            transform.position += moveDirection * (moveSpeed * Time.deltaTime);
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
