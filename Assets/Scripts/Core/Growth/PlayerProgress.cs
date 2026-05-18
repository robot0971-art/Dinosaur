using System;

namespace DinoGrow.Core.Growth
{
    public sealed class PlayerProgress
    {
        public const int DefaultStartLevel = 1;
        public const int DefaultMaxLevel = 20;
        public const int DefaultExpToLevelUp = 100;

        public int Level { get; private set; }
        public int CurrentExp { get; private set; }
        public int MaxLevel { get; }
        public int ExpToLevelUp { get; }

        public bool IsMaxLevel => Level >= MaxLevel;

        public PlayerProgress()
            : this(DefaultStartLevel, 0, DefaultMaxLevel, DefaultExpToLevelUp)
        {
        }

        public PlayerProgress(int startLevel, int currentExp, int maxLevel, int expToLevelUp)
        {
            if (maxLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLevel), "Max level must be greater than zero.");
            }

            if (expToLevelUp < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(expToLevelUp), "EXP to level up must be greater than zero.");
            }

            MaxLevel = maxLevel;
            ExpToLevelUp = expToLevelUp;
            Level = Math.Clamp(startLevel, 1, MaxLevel);
            CurrentExp = Math.Max(0, currentExp);
        }

        public void AddExp(int amount)
        {
            if (amount <= 0 || IsMaxLevel)
            {
                return;
            }

            CurrentExp += amount;
        }

        public bool TryLevelUp()
        {
            if (CurrentExp < ExpToLevelUp || IsMaxLevel)
            {
                return false;
            }

            CurrentExp -= ExpToLevelUp;
            Level++;

            if (IsMaxLevel)
            {
                CurrentExp = 0;
            }

            return true;
        }

        public void Reset()
        {
            Level = DefaultStartLevel;
            CurrentExp = 0;
        }
    }
}
