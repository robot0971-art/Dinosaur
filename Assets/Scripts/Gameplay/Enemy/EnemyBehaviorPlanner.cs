using DinoGrow.Core.Enemy;
using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class EnemyBehaviorPlanner
    {
        private readonly float fleeSpeedMultiplier;
        private readonly float chaseSpeedMultiplier;
        private readonly float minChaseSpeed;
        private readonly float maxChaseSpeed;
        private readonly EnemyAnimationMoveRule animationRule;

        public EnemyBehaviorPlanner(
            float fleeSpeedMultiplier,
            float chaseSpeedMultiplier,
            EnemyAnimationMoveRule animationRule,
            float minChaseSpeed = 5.4f,
            float maxChaseSpeed = 6.2f)
        {
            this.fleeSpeedMultiplier = fleeSpeedMultiplier;
            this.chaseSpeedMultiplier = chaseSpeedMultiplier;
            this.minChaseSpeed = minChaseSpeed;
            this.maxChaseSpeed = maxChaseSpeed;
            this.animationRule = animationRule;
        }

        public EnemyMovementPlan Plan(
            EnemyBehaviorIntent intent,
            Vector3 playerOffset,
            Vector3 wanderDirection,
            Vector3 fallbackChaseDirection,
            float moveSpeed)
        {
            return intent switch
            {
                EnemyBehaviorIntent.Flee => CreatePlan(GetFleeDirection(playerOffset), moveSpeed * fleeSpeedMultiplier, true),
                EnemyBehaviorIntent.Chase => CreatePlan(
                    GetChaseDirection(playerOffset, fallbackChaseDirection),
                    Mathf.Clamp(moveSpeed * chaseSpeedMultiplier, minChaseSpeed, maxChaseSpeed),
                    true),
                _ => CreatePlan(wanderDirection, moveSpeed, animationRule.IsRunning(moveSpeed))
            };
        }

        private static Vector3 GetFleeDirection(Vector3 playerOffset)
        {
            var fleeDirection = -playerOffset;
            if (fleeDirection.sqrMagnitude <= 0.001f)
            {
                fleeDirection = Random.onUnitSphere;
                fleeDirection.y = 0f;
            }

            return fleeDirection.sqrMagnitude > 0.001f ? fleeDirection.normalized : Vector3.zero;
        }

        private static Vector3 GetChaseDirection(Vector3 playerOffset, Vector3 fallbackDirection)
        {
            if (playerOffset.sqrMagnitude <= 0.001f)
            {
                fallbackDirection.y = 0f;
                return fallbackDirection.sqrMagnitude > 0.001f ? fallbackDirection.normalized : Vector3.forward;
            }

            return playerOffset.normalized;
        }

        private static EnemyMovementPlan CreatePlan(Vector3 direction, float speed, bool isRunning)
        {
            return new EnemyMovementPlan(direction, speed, isRunning);
        }
    }
}
