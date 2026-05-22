using System.Collections.Generic;
using DinoGrow.Core.Data;

namespace DinoGrow.Infrastructure.Data
{
    public sealed class PlayerDataRepository
    {
        private readonly Dictionary<string, PlayerDataRecord> recordsById = new Dictionary<string, PlayerDataRecord>();

        public PlayerDataRepository(PlayerDatabase database)
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

        public bool TryGetById(string id, out PlayerDataRecord record)
        {
            return recordsById.TryGetValue(id, out record);
        }
    }
}
