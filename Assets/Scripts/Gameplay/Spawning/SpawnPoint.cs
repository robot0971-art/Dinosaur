using UnityEngine;

namespace DinoGrow.Gameplay.Spawning
{
    public enum SpawnPointType
    {
        Player,
        Enemy
    }

    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private SpawnPointType spawnType;
        [SerializeField, Range(1, 20)] private int enemyLevel = 1;
        [SerializeField] private bool showGizmo = true;
        [SerializeField] private float gizmoSize = 1f;

        public SpawnPointType SpawnType => spawnType;
        public int EnemyLevel => enemyLevel;

        private void OnDrawGizmos()
        {
            if (!showGizmo)
            {
                return;
            }

            Gizmos.color = spawnType == SpawnPointType.Player
                ? Color.blue
                : Color.Lerp(Color.green, Color.red, enemyLevel / 20f);

            Gizmos.DrawWireSphere(transform.position, gizmoSize);

#if UNITY_EDITOR
            if (spawnType == SpawnPointType.Enemy)
            {
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * (gizmoSize + 0.5f),
                    $"Lv.{enemyLevel}");
            }
#endif
        }
    }
}
