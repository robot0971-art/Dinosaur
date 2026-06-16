using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class EnemyAnimationMoveRule
    {
        private readonly float runSpeedThreshold;
        private readonly float walkReferenceSpeed;
        private readonly float runReferenceSpeed;

        public EnemyAnimationMoveRule(float runSpeedThreshold, float walkReferenceSpeed, float runReferenceSpeed)
        {
            this.runSpeedThreshold = runSpeedThreshold;
            this.walkReferenceSpeed = walkReferenceSpeed;
            this.runReferenceSpeed = runReferenceSpeed;
        }

        public bool IsRunning(float speed)
        {
            return speed >= runSpeedThreshold;
        }

        public float GetMoveBlend(float speed, bool isRunning)
        {
            if (speed <= 0.001f)
            {
                return 0f;
            }

            return isRunning ? 1f : 0.5f;
        }

        public float GetPlaybackSpeed(float speed, bool isRunning)
        {
            var referenceSpeed = isRunning
                ? Mathf.Max(0.1f, runReferenceSpeed)
                : Mathf.Max(0.1f, walkReferenceSpeed);
            return Mathf.Clamp(speed / referenceSpeed, 0.75f, 1.6f);
        }
    }
}
