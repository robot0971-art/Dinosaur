using System.Collections.Generic;
using DinoGrow.Core.Data;

namespace DinoGrow.Infrastructure.Data
{
    public sealed class ItemDataRepository
    {
        private readonly Dictionary<string, ItemDataRecord> recordsById = new Dictionary<string, ItemDataRecord>();

        public ItemDataRepository(ItemDatabase database)
        {
            if (database == null)
            {
                return;
            }

            foreach (var record in database.Records)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.id))
                {
                    continue;
                }

                recordsById[record.id] = record;
            }
        }

        public bool TryGetById(string id, out ItemDataRecord record)
        {
            return recordsById.TryGetValue(id, out record);
        }
    }
}
