using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class EnemyWanderDirectionRule
    {
        private readonly float directionChangeInterval;

        public EnemyWanderDirectionRule(float directionChangeInterval)
        {
            this.directionChangeInterval = directionChangeInterval;
        }

        public Vector3 PickDirection(Vector3 position, EnemyAreaMovementRule areaRule)
        {
            if (areaRule.TryGetInwardDirection(position, out var inwardDirection))
            {
                return inwardDirection;
            }

            var random = Random.insideUnitCircle.normalized;
            return new Vector3(random.x, 0f, random.y);
        }

        public float GetNextDirectionTime(float now)
        {
            return now + Random.Range(directionChangeInterval * 0.7f, directionChangeInterval * 1.3f);
        }
    }
}
