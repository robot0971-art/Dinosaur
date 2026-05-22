using System.Collections.Generic;
using UnityEngine;

namespace DinoGrow.Core.Data
{
    [CreateAssetMenu(menuName = "Dino Grow/Data/Stage Database", fileName = "StageDatabase")]
    public sealed class StageDatabase : ScriptableObject
    {
        [SerializeField] private List<StageDataRecord> records = new List<StageDataRecord>();

        public IReadOnlyList<StageDataRecord> Records => records;

        public void SetRecords(IEnumerable<StageDataRecord> source)
        {
            records = new List<StageDataRecord>(source);
        }
    }
}
