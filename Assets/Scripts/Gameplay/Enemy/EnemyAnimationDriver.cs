using DinoGrow.Gameplay.Animation;
using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class EnemyAnimationDriver
    {
        private readonly DinoAnimatorView animatorView;
        private readonly EnemyAnimationMoveRule animationRule;
        private readonly Transform ownerTransform;
        private readonly float farAnimationDistance;
        private readonly float farAnimationUpdateInterval;
        private bool farAnimationUpdates;

        public EnemyAnimationDriver(
            DinoAnimatorView animatorView,
            EnemyAnimationMoveRule animationRule,
            Transform ownerTransform,
            float farAnimationDistance,
            float farAnimationUpdateInterval)
        {
            this.animatorView = animatorView;
            this.animationRule = animationRule;
            this.ownerTransform = ownerTransform;
            this.farAnimationDistance = farAnimationDistance;
            this.farAnimationUpdateInterval = farAnimationUpdateInterval;
        }

        public void Stop()
        {
            animatorView?.SetMove(0f, false);
            animatorView?.SetPlaybackSpeed(1f);
        }

        public void StopMoveOnly()
        {
            animatorView?.SetMove(0f, false);
        }

        public void ApplyMovementPlan(EnemyMovementPlan plan)
        {
            if (animationRule == null)
            {
                animatorView?.SetMove(plan.Speed, plan.IsRunning);
                return;
            }

            animatorView?.SetMove(animationRule.GetMoveBlend(plan.Speed, plan.IsRunning), plan.IsRunning);
            animatorView?.SetPlaybackSpeed(animationRule.GetPlaybackSpeed(plan.Speed, plan.IsRunning));
        }

        public void UpdateDetailLevel(Transform player)
        {
            if (animatorView == null || ownerTransform == null || player == null)
            {
                return;
            }

            var offset = player.position - ownerTransform.position;
            offset.y = 0f;
            var shouldUseFarUpdates = offset.sqrMagnitude >= farAnimationDistance * farAnimationDistance;
            if (shouldUseFarUpdates == farAnimationUpdates)
            {
                return;
            }

            farAnimationUpdates = shouldUseFarUpdates;
            animatorView.SetLowDetailUpdates(farAnimationUpdates, farAnimationUpdateInterval);
        }
    }
}
