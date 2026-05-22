using System.Collections.Generic;
using DinoGrow.Core.Data;

namespace DinoGrow.Infrastructure.Data
{
    public sealed class SpawnDataRepository
    {
        private readonly Dictionary<int, List<SpawnDataRecord>> recordsByStageId = new Dictionary<int, List<SpawnDataRecord>>();

        public SpawnDataRepository(SpawnDatabase database)
        {
            if (database == null)
            {
                return;
            }

            foreach (var record in database.Records)
            {
                if (record == null || record.stageId <= 0 || string.IsNullOrWhiteSpace(record.dinoId))
                {
                    continue;
                }

                if (!recordsByStageId.TryGetValue(record.stageId, out var stageRecords))
                {
                    stageRecords = new List<SpawnDataRecord>();
                    recordsByStageId.Add(record.stageId, stageRecords);
                }

                stageRecords.Add(record);
            }
        }

        public IReadOnlyList<SpawnDataRecord> GetByStageId(int stageId)
        {
            return recordsByStageId.TryGetValue(stageId, out var records)
                ? records
                : System.Array.Empty<SpawnDataRecord>();
        }
    }
}
