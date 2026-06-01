using DinoGrow.Core.Enemy;
using DinoGrow.Gameplay.Player;
using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class EnemyPlayerBehaviorSensor
    {
        private readonly EnemyBehaviorResolver behaviorResolver;
        private readonly float fleeDetectDistance;
        private readonly float chaseDetectDistance;
        private readonly float chaseStopDistance;

        public EnemyPlayerBehaviorSensor(
            EnemyBehaviorResolver behaviorResolver,
            float fleeDetectDistance,
            float chaseDetectDistance,
            float chaseStopDistance)
        {
            this.behaviorResolver = behaviorResolver;
            this.fleeDetectDistance = fleeDetectDistance;
            this.chaseDetectDistance = chaseDetectDistance;
            this.chaseStopDistance = chaseStopDistance;
        }

        public EnemyBehaviorIntent Resolve(
            DinoEnemy enemy,
            Transform enemyTransform,
            Transform player,
            PlayerDinoController playerController,
            bool isChasingPlayer,
            out Vector3 playerOffset)
        {
            playerOffset = Vector3.zero;
            if (behaviorResolver == null
                || enemy == null
                || enemyTransform == null
                || player == null
                || playerController == null)
            {
                return EnemyBehaviorIntent.Wander;
            }

            playerOffset = player.position - enemyTransform.position;
            playerOffset.y = 0f;

            return behaviorResolver.Resolve(
                enemy.Level,
                playerController.Level,
                playerOffset.magnitude,
                fleeDetectDistance,
                chaseDetectDistance,
                chaseStopDistance,
                isChasingPlayer);
        }
    }
}
