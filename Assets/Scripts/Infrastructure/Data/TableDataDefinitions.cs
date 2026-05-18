using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dino.Infrastructure.Data
{
    [Serializable]
    public sealed class DinoTableEntry
    {
        public int ID;
        public string Name;
        public int Level;
        public float MoveSpeed;
        public float Scale;
        public int ExpReward;
        public string PrefabPath;
        public string Description;
    }

    [Serializable]
    public sealed class GrowthTableEntry
    {
        public int Level;
        public int RequiredExp;
        public float ScaleMultiplier;
        public float MoveSpeed;
        public float CameraDistance;
        public float CameraHeight;
    }

    [Serializable]
    public sealed class SpawnTableEntry
    {
        public int ID;
        public int StageID;
        public int DinoID;
        public int MinLevel;
        public int MaxLevel;
        public float SpawnWeight;
        public float SpawnRadius;
    }

    [Serializable]
    public sealed class StageTableEntry
    {
        public int ID;
        public string Name;
        public int MinPlayerLevel;
        public int MaxPlayerLevel;
        public float MapSize;
        public string SceneName;
    }

    public interface ITableData<T>
    {
        void SetEntries(List<T> entries);
        List<T> GetEntries();
    }

    public sealed class DinoTableData : ScriptableObject, ITableData<DinoTableEntry>
    {
        public List<DinoTableEntry> entries = new List<DinoTableEntry>();

        public void SetEntries(List<DinoTableEntry> newEntries)
        {
            entries = newEntries;
        }

        public List<DinoTableEntry> GetEntries()
        {
            return entries;
        }
    }

    public sealed class GrowthTableData : ScriptableObject, ITableData<GrowthTableEntry>
    {
        public List<GrowthTableEntry> entries = new List<GrowthTableEntry>();

        public void SetEntries(List<GrowthTableEntry> newEntries)
        {
            entries = newEntries;
        }

        public List<GrowthTableEntry> GetEntries()
        {
            return entries;
        }
    }

    public sealed class SpawnTableData : ScriptableObject, ITableData<SpawnTableEntry>
    {
        public List<SpawnTableEntry> entries = new List<SpawnTableEntry>();

        public void SetEntries(List<SpawnTableEntry> newEntries)
        {
            entries = newEntries;
        }

        public List<SpawnTableEntry> GetEntries()
        {
            return entries;
        }
    }

    public sealed class StageTableData : ScriptableObject, ITableData<StageTableEntry>
    {
        public List<StageTableEntry> entries = new List<StageTableEntry>();

        public void SetEntries(List<StageTableEntry> newEntries)
        {
            entries = newEntries;
        }

        public List<StageTableEntry> GetEntries()
        {
            return entries;
        }
    }
}
