using System.Collections.Generic;
using DinoGrow.Core.Data;

namespace DinoGrow.Infrastructure.Data
{
    public interface IDataService
    {
        IReadOnlyList<PlayerDataRecord> LoadPlayerRows(string xlsxPath);
        IReadOnlyList<DinoDataRecord> LoadEnemyDinoRows(string xlsxPath);
        IReadOnlyList<ItemDataRecord> LoadItemRows(string xlsxPath);
        IReadOnlyList<StageDataRecord> LoadStageRows(string xlsxPath);
        IReadOnlyList<SpawnDataRecord> LoadSpawnRows(string xlsxPath);
        IReadOnlyList<PlayerGrowthDataRecord> LoadPlayerGrowthRows(string xlsxPath);
        void CreateDinoTemplate(string xlsxPath);
        void CreateGameDataTemplate(string xlsxPath);
    }
}
