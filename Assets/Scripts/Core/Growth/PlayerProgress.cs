namespace DinoGrow.Core.Growth
{
    public sealed class PlayerProgress
    {
        public static int DefaultStartLevel = 1;
        public static int DefaultMaxLevel = 20;
        public static int DefaultExpToLevelUp = 100;

        public int Level { get; private set; }
        public int CurrentExp { get; private set; }
        public int MaxLevel { get; }
        public int ExpToLevelUp { get; }
        public bool IsMaxLevel => Level >= MaxLevel;

        public PlayerProgress(int startLevel, int startExp, int maxLevel, int expToLevelUp)
        {
            Level = startLevel;
            CurrentExp = startExp;
            MaxLevel = maxLevel;
            ExpToLevelUp = expToLevelUp;
        }

        public void SetLevel(int level)
        {
            Level = UnityEngine.Mathf.Clamp(level, 1, MaxLevel);
        }

        public void SetExp(int exp)
        {
            CurrentExp = UnityEngine.Mathf.Clamp(exp, 0, 999);
        }
    }
}
