using System.Collections.Generic;
using UnityEngine;

namespace DinoGrow.Core.Data
{
    [CreateAssetMenu(menuName = "Dino Grow/Data/Item Database", fileName = "ItemDatabase")]
    public sealed class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDataRecord> records = new List<ItemDataRecord>();

        public IReadOnlyList<ItemDataRecord> Records => records;

        public void SetRecords(IEnumerable<ItemDataRecord> source)
        {
            records = new List<ItemDataRecord>(source);
        }
    }
}
