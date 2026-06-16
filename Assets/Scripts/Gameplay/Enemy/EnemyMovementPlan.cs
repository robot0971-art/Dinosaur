using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    public readonly struct EnemyMovementPlan
    {
        public EnemyMovementPlan(Vector3 direction, float speed, bool isRunning)
        {
            Direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
            Speed = Mathf.Max(0f, speed);
            IsRunning = isRunning;
        }

        public Vector3 Direction { get; }
        public float Speed { get; }
        public bool IsRunning { get; }

        public static EnemyMovementPlan Stop => new(Vector3.zero, 0f, false);
    }
}
