using System.Collections.Generic;
using UnityEngine;

namespace DinoGrow.Core.Data
{
    [CreateAssetMenu(menuName = "Dino Grow/Data/Spawn Database", fileName = "SpawnDatabase")]
    public sealed class SpawnDatabase : ScriptableObject
    {
        [SerializeField] private List<SpawnDataRecord> records = new List<SpawnDataRecord>();

        public IReadOnlyList<SpawnDataRecord> Records => records;

        public void SetRecords(IEnumerable<SpawnDataRecord> source)
        {
            records = new List<SpawnDataRecord>(source);
        }
    }
}
