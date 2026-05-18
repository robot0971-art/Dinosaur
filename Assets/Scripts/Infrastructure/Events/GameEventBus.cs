using System;
using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;

namespace DinoGrow.Infrastructure.Events
{
    public sealed class GameEventBus
    {
        public event Action<int, int> EnemyEaten;
        public event Action<GrowthResult> PlayerGrowthChanged;
        public event Action<GameState> GameStateChanged;

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
    }
}
