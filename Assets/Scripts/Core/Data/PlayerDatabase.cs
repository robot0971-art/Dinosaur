using System.Collections.Generic;
using UnityEngine;

namespace DinoGrow.Core.Data
{
    [CreateAssetMenu(menuName = "Dino Grow/Data/Player Database", fileName = "PlayerDatabase")]
    public sealed class PlayerDatabase : ScriptableObject
    {
        [SerializeField] private List<PlayerDataRecord> records = new List<PlayerDataRecord>();

        public IReadOnlyList<PlayerDataRecord> Records => records;

        public void SetRecords(IEnumerable<PlayerDataRecord> source)
        {
            records = new List<PlayerDataRecord>(source);
        }
    }
}
