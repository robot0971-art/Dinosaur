namespace DinoGrow.Core.Enemy
{
    public sealed class EnemyBehaviorResolver
    {
        public EnemyBehaviorIntent Resolve(
            int enemyLevel,
            int playerLevel,
            float distanceToPlayer,
            float fleeDetectDistance,
            float chaseDetectDistance,
            float chaseStopDistance,
            bool isChasingPlayer)
        {
            if (enemyLevel < playerLevel && distanceToPlayer <= fleeDetectDistance)
            {
                return EnemyBehaviorIntent.Flee;
            }

            if (enemyLevel > playerLevel)
            {
                var activeChaseDistance = isChasingPlayer
                    ? Max(chaseDetectDistance, chaseStopDistance)
                    : chaseDetectDistance;

                if (distanceToPlayer <= activeChaseDistance)
                {
                    return EnemyBehaviorIntent.Chase;
                }
            }

            return EnemyBehaviorIntent.Wander;
        }

        private static float Max(float left, float right)
        {
            return left > right ? left : right;
        }
    }
}
