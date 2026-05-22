using System.Collections.Generic;
using DinoGrow.Core.Data;

namespace DinoGrow.Infrastructure.Data
{
    public sealed class StageDataRepository
    {
        private readonly Dictionary<int, StageDataRecord> recordsByStageId = new Dictionary<int, StageDataRecord>();

        public StageDataRepository(StageDatabase database)
        {
            if (database == null)
            {
                return;
            }

            foreach (var record in database.Records)
            {
                if (record == null || record.stageId <= 0)
                {
                    continue;
                }

                recordsByStageId[record.stageId] = record;
            }
        }

        public bool TryGetByStageId(int stageId, out StageDataRecord record)
        {
            return recordsByStageId.TryGetValue(stageId, out record);
        }
    }
}
