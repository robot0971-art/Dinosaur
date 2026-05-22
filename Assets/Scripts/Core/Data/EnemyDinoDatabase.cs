using System.Collections.Generic;
using UnityEngine;

namespace DinoGrow.Core.Data
{
    [CreateAssetMenu(menuName = "Dino Grow/Data/Enemy Dino Database", fileName = "EnemyDinoDatabase")]
    public sealed class EnemyDinoDatabase : ScriptableObject
    {
        [SerializeField] private List<DinoDataRecord> records = new List<DinoDataRecord>();

        public IReadOnlyList<DinoDataRecord> Records => records;

        public void SetRecords(IEnumerable<DinoDataRecord> source)
        {
            records = new List<DinoDataRecord>(source);
        }
    }
}
