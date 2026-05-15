namespace DinoGrow.Core.Growth
{
    public readonly struct GrowthResult
    {
        public readonly int GainedExp;
        public readonly int LevelUpCount;
        public readonly int CurrentLevel;
        public readonly int CurrentExp;
        public readonly bool ReachedMaxLevel;

        public bool LeveledUp => LevelUpCount > 0;

        public GrowthResult(int gainedExp, int levelUpCount, int currentLevel, int currentExp, bool reachedMaxLevel)
        {
            GainedExp = gainedExp;
            LevelUpCount = levelUpCount;
            CurrentLevel = currentLevel;
            CurrentExp = currentExp;
            ReachedMaxLevel = reachedMaxLevel;
        }
    }
}
