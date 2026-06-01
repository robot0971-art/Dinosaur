using System;
using DinoGrow.Core.Data;

namespace DinoGrow.Core.Enemy
{
    public sealed class EnemySpawnLevelRule
    {
        public int GetSpawnLevel(
            SpawnDataRecord spawnRecord,
            EnemySpawnLevelContext context,
            Func<float> random01,
            Func<int, int, int> randomRangeInclusive)
        {
            if (spawnRecord == null)
            {
                return 1;
            }

            var minLevel = Math.Max(1, spawnRecord.minLevel);
            var maxLevel = Math.Max(minLevel, spawnRecord.maxLevel);

            if (!context.ScaleWithPlayer || !context.HasPlayerProgress)
            {
                return randomRangeInclusive(minLevel, maxLevel);
            }

            var playerLevel = Math.Max(1, context.PlayerLevel);
            var maxPossibleLevel = Math.Max(1, context.MaxPlayerLevel);
            var shouldSpawnThreat = playerLevel < maxPossibleLevel && random01() < context.ThreatSpawnChance;
            if (shouldSpawnThreat)
            {
                minLevel = Math.Max(minLevel, playerLevel + 1);
                maxLevel = Math.Max(minLevel, playerLevel + context.ThreatLevelOffset);
            }
            else
            {
                minLevel = Math.Max(1, playerLevel - context.EdibleLevelOffset);
                maxLevel = Math.Max(minLevel, playerLevel);
            }

            minLevel = Clamp(minLevel, 1, maxPossibleLevel);
            maxLevel = Clamp(maxLevel, minLevel, maxPossibleLevel);
            return randomRangeInclusive(minLevel, maxLevel);
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }

    public readonly struct EnemySpawnLevelContext
    {
        public EnemySpawnLevelContext(
            bool scaleWithPlayer,
            bool hasPlayerProgress,
            int playerLevel,
            int maxPlayerLevel,
            int edibleLevelOffset,
            int threatLevelOffset,
            float threatSpawnChance)
        {
            ScaleWithPlayer = scaleWithPlayer;
            HasPlayerProgress = hasPlayerProgress;
            PlayerLevel = playerLevel;
            MaxPlayerLevel = maxPlayerLevel;
            EdibleLevelOffset = edibleLevelOffset;
            ThreatLevelOffset = threatLevelOffset;
            ThreatSpawnChance = Clamp(threatSpawnChance, 0f, 1f);
        }

        public bool ScaleWithPlayer { get; }
        public bool HasPlayerProgress { get; }
        public int PlayerLevel { get; }
        public int MaxPlayerLevel { get; }
        public int EdibleLevelOffset { get; }
        public int ThreatLevelOffset { get; }
        public float ThreatSpawnChance { get; }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }
}
