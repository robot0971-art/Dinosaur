using System;
using DinoGrow.Core.Data;

namespace DinoGrow.Core.Enemy
{
    public sealed class EnemySpawnStatsRule
    {
        private const float LevelSizeBonus = 0.08f;

        public float GetEnemySize(DinoDataRecord dinoData, int level)
        {
            if (dinoData == null)
            {
                return 1f;
            }

            var baseSize = dinoData.size > 0f ? dinoData.size : 1f;
            var baseLevel = Math.Max(1, dinoData.level);
            var levelBonus = Math.Max(0, level - baseLevel) * LevelSizeBonus;
            return baseSize * (1f + levelBonus);
        }

        public float GetMoveSpeed(
            DinoDataRecord dinoData,
            SpawnDataRecord spawnRecord,
            float fallbackMinSpeed,
            float fallbackMaxSpeed,
            Func<float, float, float> randomRange)
        {
            var minFallback = Math.Max(0f, fallbackMinSpeed);
            var maxFallback = Math.Max(minFallback, fallbackMaxSpeed);
            if (spawnRecord != null && spawnRecord.maxWanderSpeed > 0f)
            {
                var minSpeed = Math.Max(minFallback, spawnRecord.minWanderSpeed);
                var maxSpeed = Math.Max(Math.Max(minSpeed, spawnRecord.maxWanderSpeed), maxFallback);
                return randomRange(minSpeed, maxSpeed);
            }

            var dataSpeed = dinoData != null ? dinoData.speed : minFallback;
            return Math.Min(Math.Max(dataSpeed, minFallback), maxFallback);
        }
    }
}
