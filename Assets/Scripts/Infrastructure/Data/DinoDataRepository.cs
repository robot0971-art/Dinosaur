using System.Collections.Generic;
using DinoGrow.Core.Data;

namespace DinoGrow.Infrastructure.Data
{
    public sealed class DinoDataRepository
    {
        private readonly Dictionary<string, DinoDataRecord> recordsById = new Dictionary<string, DinoDataRecord>();

        public DinoDataRepository(DinoDatabase database)
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

        public bool TryGetById(string id, out DinoDataRecord record)
        {
            return recordsById.TryGetValue(id, out record);
        }
    }
}
