using UnityEngine;

public class EnemySpawnData : MonoBehaviour
{
    public int level;
}

public enum SpawnPointType
{
    Player,
    Enemy
}

public class SpawnPoint : MonoBehaviour
{
    [Header("Spawn Point")]
    public SpawnPointType spawnType;

    [Range(1, 20)]
    public int enemyLevel = 1;

    [Header("Gizmos")]
    public bool showGizmo = true;
    public float gizmoSize = 1f;

    private void OnDrawGizmos()
    {
        if (!showGizmo)
        {
            return;
        }

        switch (spawnType)
        {
            case SpawnPointType.Player:
                Gizmos.color = Color.blue;
                break;
            case SpawnPointType.Enemy:
                var t = enemyLevel / 20f;
                Gizmos.color = Color.Lerp(Color.green, Color.red, t);
                break;
        }

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

public class MapSettings : MonoBehaviour
{
    [Header("Map")]
    public string mapName = "Grassland";
    public float mapSize = 100f;

    [Header("Player Start")]
    public Transform playerStartPoint;

    [Header("Enemy Spawn Points")]
    public Transform[] enemySpawnPoints;

    public Transform[] GetEnemySpawnPointsByLevel(int level)
    {
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            return System.Array.Empty<Transform>();
        }

        var filtered = new System.Collections.Generic.List<Transform>();
        foreach (var spawnPoint in enemySpawnPoints)
        {
            if (spawnPoint == null)
            {
                continue;
            }

            var data = spawnPoint.GetComponent<GrasslandEnemySpawnData>();
            if (data != null && data.Level == level)
            {
                filtered.Add(spawnPoint);
            }
        }

        return filtered.ToArray();
    }
}
