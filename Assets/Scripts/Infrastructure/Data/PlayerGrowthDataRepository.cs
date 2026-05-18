using System.Collections.Generic;
using DinoGrow.Core.Data;

namespace DinoGrow.Infrastructure.Data
{
    public sealed class PlayerGrowthDataRepository
    {
        private readonly Dictionary<int, PlayerGrowthDataRecord> recordsByLevel = new Dictionary<int, PlayerGrowthDataRecord>();

        public PlayerGrowthDataRepository(PlayerGrowthDatabase database)
        {
            if (database == null)
            {
                return;
            }

            foreach (var record in database.Records)
            {
                if (record == null || record.level <= 0)
                {
                    continue;
                }

                recordsByLevel[record.level] = record;
            }
        }

        public bool TryGetByLevel(int level, out PlayerGrowthDataRecord record)
        {
            return recordsByLevel.TryGetValue(level, out record);
        }
    }
}
