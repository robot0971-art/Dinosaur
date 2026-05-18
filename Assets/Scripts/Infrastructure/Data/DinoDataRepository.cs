using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dino.Infrastructure.Data
{
    public interface IDinoDataRepository
    {
        DinoTableEntry GetDinoData(int id);
        DinoTableEntry GetDinoDataByLevel(int level);
        List<DinoTableEntry> GetAllDinoData();
        GrowthTableEntry GetGrowthData(int level);
        List<GrowthTableEntry> GetAllGrowthData();
        SpawnTableEntry GetSpawnData(int id);
        List<SpawnTableEntry> GetSpawnDataByStage(int stageId);
        StageTableEntry GetStageData(int id);
        List<StageTableEntry> GetAllStageData();
    }

    public class DinoDataRepository : IDinoDataRepository
    {
        private readonly IDataService _dataService;
        private DinoTableData _dinoTable;
        private GrowthTableData _growthTable;
        private SpawnTableData _spawnTable;
        private StageTableData _stageTable;

        public DinoDataRepository(IDataService dataService)
        {
            _dataService = dataService;
            LoadAllData();
        }

        private void LoadAllData()
        {
            _dinoTable = Resources.Load<DinoTableData>("GameData/Generated/DinoTable");
            _growthTable = Resources.Load<GrowthTableData>("GameData/Generated/GrowthTable");
            _spawnTable = Resources.Load<SpawnTableData>("GameData/Generated/SpawnTable");
            _stageTable = Resources.Load<StageTableData>("GameData/Generated/StageTable");

            if (_dinoTable == null)
                Debug.LogWarning("DinoTable not found in Resources");
            if (_growthTable == null)
                Debug.LogWarning("GrowthTable not found in Resources");
            if (_spawnTable == null)
                Debug.LogWarning("SpawnTable not found in Resources");
            if (_stageTable == null)
                Debug.LogWarning("StageTable not found in Resources");
        }

        public DinoTableEntry GetDinoData(int id)
        {
            if (_dinoTable == null) return null;
            return _dinoTable.entries.FirstOrDefault(e => e.ID == id);
        }

        public DinoTableEntry GetDinoDataByLevel(int level)
        {
            if (_dinoTable == null) return null;
            return _dinoTable.entries.FirstOrDefault(e => e.Level == level);
        }

        public List<DinoTableEntry> GetAllDinoData()
        {
            if (_dinoTable == null) return new List<DinoTableEntry>();
            return _dinoTable.entries;
        }

        public GrowthTableEntry GetGrowthData(int level)
        {
            if (_growthTable == null) return null;
            return _growthTable.entries.FirstOrDefault(e => e.Level == level);
        }

        public List<GrowthTableEntry> GetAllGrowthData()
        {
            if (_growthTable == null) return new List<GrowthTableEntry>();
            return _growthTable.entries;
        }

        public SpawnTableEntry GetSpawnData(int id)
        {
            if (_spawnTable == null) return null;
            return _spawnTable.entries.FirstOrDefault(e => e.ID == id);
        }

        public List<SpawnTableEntry> GetSpawnDataByStage(int stageId)
        {
            if (_spawnTable == null) return new List<SpawnTableEntry>();
            return _spawnTable.entries.Where(e => e.StageID == stageId).ToList();
        }

        public StageTableEntry GetStageData(int id)
        {
            if (_stageTable == null) return null;
            return _stageTable.entries.FirstOrDefault(e => e.ID == id);
        }

        public List<StageTableEntry> GetAllStageData()
        {
            if (_stageTable == null) return new List<StageTableEntry>();
            return _stageTable.entries;
        }
    }
}