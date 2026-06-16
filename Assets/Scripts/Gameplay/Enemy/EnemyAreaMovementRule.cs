using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class EnemyAreaMovementRule
    {
        private Vector3 center;
        private Vector2 size;

        public EnemyAreaMovementRule(Vector3 center, Vector2 size)
        {
            Configure(center, size);
        }

        public void Configure(Vector3 newCenter, Vector2 newSize)
        {
            center = newCenter;
            size = new Vector2(Mathf.Max(1f, newSize.x), Mathf.Max(1f, newSize.y));
        }

        public bool IsNearEdge(Vector3 position)
        {
            var halfSize = size * 0.5f;
            var local = position - center;
            return Mathf.Abs(local.x) > halfSize.x * 0.9f || Mathf.Abs(local.z) > halfSize.y * 0.9f;
        }

        public bool TryGetInwardDirection(Vector3 position, out Vector3 inwardDirection)
        {
            if (!IsNearEdge(position))
            {
                inwardDirection = Vector3.zero;
                return false;
            }

            inwardDirection = center - position;
            inwardDirection.y = 0f;
            if (inwardDirection.sqrMagnitude <= 0.001f)
            {
                inwardDirection = Vector3.zero;
                return false;
            }

            inwardDirection.Normalize();
            return true;
        }

        public Vector3 Clamp(Vector3 position)
        {
            var halfSize = size * 0.5f;
            position.x = Mathf.Clamp(position.x, center.x - halfSize.x, center.x + halfSize.x);
            position.z = Mathf.Clamp(position.z, center.z - halfSize.y, center.z + halfSize.y);
            return position;
        }

        public Vector3 GetSafeDirection(Vector3 position, Vector3 direction)
        {
            if (!IsMovingOutOfArea(position, direction))
            {
                return direction;
            }

            return TryGetInwardDirection(position, out var inwardDirection)
                ? inwardDirection
                : -direction;
        }

        private bool IsMovingOutOfArea(Vector3 position, Vector3 direction)
        {
            var halfSize = size * 0.5f;
            var local = position - center;
            var nearXEdge = Mathf.Abs(local.x) > halfSize.x * 0.88f;
            var nearZEdge = Mathf.Abs(local.z) > halfSize.y * 0.88f;

            if (nearXEdge && Mathf.Sign(local.x) == Mathf.Sign(direction.x))
            {
                return true;
            }

            return nearZEdge && Mathf.Sign(local.z) == Mathf.Sign(direction.z);
        }
    }
}
