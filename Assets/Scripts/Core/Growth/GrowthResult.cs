namespace DinoGrow.Core.Growth
{
    public struct GrowthResult
    {
        public int PreviousLevel;
        public int PreviousExp;
        public int CurrentLevel;
        public int CurrentExp;
        public bool LeveledUp;
        public bool IsMaxLevel;
        public int GainedExp;

        public GrowthResult(int previousLevel, int previousExp, int currentLevel, int currentExp, bool isMaxLevel)
        {
            PreviousLevel = previousLevel;
            PreviousExp = previousExp;
            CurrentLevel = currentLevel;
            CurrentExp = currentExp;
            IsMaxLevel = isMaxLevel;
            LeveledUp = currentLevel > previousLevel;
            GainedExp = currentExp - previousExp;
            if (GainedExp < 0) GainedExp = 0;
        }
    }
}
