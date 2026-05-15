using System;

namespace DinoGrow.Core.Growth
{
    public sealed class GrowthSystem
    {
        private const int ExpMultiplier = 10;

        public int CalculateExpReward(int enemyLevel)
        {
            return Math.Max(0, enemyLevel) * ExpMultiplier;
        }

        public GrowthResult AddEnemyExp(PlayerProgress progress, int enemyLevel)
        {
            var gainedExp = CalculateExpReward(enemyLevel);
            progress.AddExp(gainedExp);

            var levelUpCount = 0;
            while (progress.TryLevelUp())
            {
                levelUpCount++;
            }

            return new GrowthResult(
                gainedExp,
                levelUpCount,
                progress.Level,
                progress.CurrentExp,
                progress.IsMaxLevel);
        }
    }
}
