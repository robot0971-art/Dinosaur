using UnityEngine;

public sealed class SnowEnemySpawnData : MonoBehaviour
{
    [Range(1, 20)] public int level = 1;

    public int Level => level;

    public void SetLevel(int value)
    {
        level = Mathf.Clamp(value, 1, 20);
    }
}
