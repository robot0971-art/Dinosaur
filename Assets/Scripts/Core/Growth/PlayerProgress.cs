using System;
using System.Collections.Generic;

namespace DinoGrow.Core.Growth
{
    public sealed class PlayerProgress
    {
        public const int DefaultStartLevel = 1;
        public const int DefaultMaxLevel = 20;
        public const int DefaultExpToLevelUp = 50;

        public int Level { get; private set; }
        public int CurrentExp { get; private set; }
        public int MaxLevel { get; }
        public int ExpToLevelUp => GetExpToLevelUp(Level);

        public bool IsMaxLevel => Level >= MaxLevel;

        private readonly IReadOnlyDictionary<int, int> requiredExpByLevel;

        public PlayerProgress()
            : this(DefaultStartLevel, 0, DefaultMaxLevel, DefaultExpToLevelUp)
        {
        }

        public PlayerProgress(int startLevel, int currentExp, int maxLevel, int expToLevelUp)
            : this(startLevel, currentExp, maxLevel, expToLevelUp, null)
        {
        }

        public PlayerProgress(
            int startLevel,
            int currentExp,
            int maxLevel,
            int defaultExpToLevelUp,
            IReadOnlyDictionary<int, int> requiredExpByLevel)
        {
            if (maxLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLevel), "Max level must be greater than zero.");
            }

            if (defaultExpToLevelUp < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(defaultExpToLevelUp), "EXP to level up must be greater than zero.");
            }

            MaxLevel = maxLevel;
            this.requiredExpByLevel = requiredExpByLevel;
            DefaultRequiredExp = defaultExpToLevelUp;
            Level = Math.Clamp(startLevel, 1, MaxLevel);
            CurrentExp = Math.Max(0, currentExp);
        }

        private int DefaultRequiredExp { get; }

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

        private int GetExpToLevelUp(int level)
        {
            if (requiredExpByLevel != null
                && requiredExpByLevel.TryGetValue(level, out var requiredExp)
                && requiredExp > 0)
            {
                return requiredExp;
            }

            return DefaultRequiredExp;
        }

        public void Reset()
        {
            Level = DefaultStartLevel;
            CurrentExp = 0;
        }
    }
}
