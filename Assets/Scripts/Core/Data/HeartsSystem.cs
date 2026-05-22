using System;
using DinoGrow.Infrastructure.Events;

namespace DinoGrow.Core.Data
{
    public sealed class HeartsSystem
    {
        private readonly GameEventBus eventBus;
        private int maxLives;
        private int currentLives;

        public int MaxLives => maxLives;
        public int CurrentLives => currentLives;
        public bool IsAlive => currentLives > 0;
        public bool IsDead => currentLives <= 0;

        public HeartsSystem(GameEventBus eventBus)
        {
            this.eventBus = eventBus;
            maxLives = 3;
            currentLives = maxLives;
        }

        public void Initialize(int maxLives)
        {
            this.maxLives = Math.Max(1, maxLives);
            currentLives = this.maxLives;
            eventBus?.PublishHeartsChanged(currentLives, this.maxLives);
        }

        public bool LoseLife()
        {
            if (currentLives <= 0)
            {
                return false;
            }

            currentLives--;
            eventBus?.PublishHeartsChanged(currentLives, maxLives);

            if (currentLives <= 0)
            {
                eventBus?.PublishPlayerDeath();
            }

            return true;
        }

        public bool AddLife()
        {
            if (currentLives >= maxLives)
            {
                return false;
            }

            currentLives++;
            eventBus?.PublishHeartsChanged(currentLives, maxLives);
            return true;
        }

        public void ResetLives()
        {
            currentLives = maxLives;
            eventBus?.PublishHeartsChanged(currentLives, maxLives);
        }

        public void SetMaxLives(int maxLives)
        {
            this.maxLives = Math.Max(1, maxLives);
            currentLives = Math.Min(currentLives, this.maxLives);
            eventBus?.PublishHeartsChanged(currentLives, this.maxLives);
        }
    }
}
