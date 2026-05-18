using System.Collections.Generic;
using UnityEngine;

namespace DinoGrow.Core.Data
{
    [CreateAssetMenu(menuName = "Dino Grow/Data/Player Growth Database", fileName = "PlayerGrowthDatabase")]
    public sealed class PlayerGrowthDatabase : ScriptableObject
    {
        [SerializeField] private List<PlayerGrowthDataRecord> records = new List<PlayerGrowthDataRecord>();

        public IReadOnlyList<PlayerGrowthDataRecord> Records => records;

        public void SetRecords(IEnumerable<PlayerGrowthDataRecord> source)
        {
            records = new List<PlayerGrowthDataRecord>(source);
        }
    }
}
