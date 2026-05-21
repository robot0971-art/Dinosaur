using UnityEngine;

public sealed class DesertEnemySpawnData : MonoBehaviour
{
    [SerializeField, Range(1, 20)] private int level = 1;

    public int Level => level;

    public void SetLevel(int value)
    {
        level = Mathf.Clamp(value, 1, 20);
    }
}
