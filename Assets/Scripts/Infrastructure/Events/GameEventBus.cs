using System;
using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;

namespace DinoGrow.Infrastructure.Events
{
    public sealed class GameEventBus
    {
        public event Action<int> EnemySpawned;
        public event Action<int> EnemyDespawned;
        public event Action<int, int> EnemyEaten;
        public event Action<GrowthResult> PlayerGrowthChanged;
        public event Action<GameState> GameStateChanged;
        public event Action<int, int> HeartsChanged;
        public event Action PlayerDeath;

        public void PublishEnemySpawned(int enemyLevel)
        {
            EnemySpawned?.Invoke(enemyLevel);
        }

        public void PublishEnemyDespawned(int enemyLevel)
        {
            EnemyDespawned?.Invoke(enemyLevel);
        }

        public void PublishEnemyEaten(int enemyLevel, int gainedExp)
        {
            EnemyEaten?.Invoke(enemyLevel, gainedExp);
        }

        public void PublishPlayerGrowthChanged(GrowthResult result)
        {
            PlayerGrowthChanged?.Invoke(result);
        }

        public void PublishGameStateChanged(GameState state)
        {
            GameStateChanged?.Invoke(state);
        }

        public void PublishHeartsChanged(int currentLives, int maxLives)
        {
            HeartsChanged?.Invoke(currentLives, maxLives);
        }

        public void PublishPlayerDeath()
        {
            PlayerDeath?.Invoke();
        }
    }
}
