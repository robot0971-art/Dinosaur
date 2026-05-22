namespace DinoGrow.Core.Growth
{
    public sealed class GrowthSystem
    {
        public GrowthResult AddEnemyExp(PlayerProgress progress, int enemyLevel)
        {
            if (progress == null || progress.IsMaxLevel)
            {
                return new GrowthResult(
                    progress?.Level ?? 1,
                    progress?.CurrentExp ?? 0,
                    progress?.Level ?? 1,
                    progress?.CurrentExp ?? 0,
                    progress?.IsMaxLevel ?? true);
            }

            var previousLevel = progress.Level;
            var previousExp = progress.CurrentExp;
            var gainedExp = enemyLevel * 10;
            var newExp = progress.CurrentExp + gainedExp;

            while (newExp >= progress.ExpToLevelUp && progress.Level < progress.MaxLevel)
            {
                newExp -= progress.ExpToLevelUp;
                progress.SetLevel(progress.Level + 1);
            }

            progress.SetExp(newExp);

            return new GrowthResult(
                previousLevel,
                previousExp,
                progress.Level,
                progress.CurrentExp,
                progress.IsMaxLevel)
            {
                GainedExp = gainedExp
            };
        }
    }
}
