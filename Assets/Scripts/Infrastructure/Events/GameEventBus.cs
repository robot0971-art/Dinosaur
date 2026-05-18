using System;
using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;

namespace DinoGrow.Infrastructure.Events
{
    /// <summary>
    /// 게임에서 발생한 일을 다른 스크립트에게 알려주는 이벤트 전달 클래스입니다.
    /// MonoBehaviour가 아니며, GameLifetimeScope에서 VContainer로 주입받아 사용합니다.
    /// </summary>
    public sealed class GameEventBus
    {
        // 기존 코어/게임플레이 코드가 사용 중인 이벤트입니다.
        public event Action<int, int> EnemyEaten;
        public event Action<GrowthResult> PlayerGrowthChanged;
        public event Action<GameState> GameStateChanged;

        // UI, 사운드, 스테이지 시스템이 구독할 기본 이벤트입니다.
        public event Action<int> ExpChanged;
        public event Action<int> LevelChanged;
        public event Action<int> EatSuccess;
        public event Action GameOver;
        public event Action GameClear;
        public event Action StageCleared;
        public event Action LevelUp;

        public void PublishEnemyEaten(int enemyLevel, int gainedExp)
        {
            EnemyEaten?.Invoke(enemyLevel, gainedExp);
            PublishEatSuccess(gainedExp);
        }

        public void PublishPlayerGrowthChanged(GrowthResult result)
        {
            PlayerGrowthChanged?.Invoke(result);
            PublishExpChanged(result.CurrentExp);
            PublishLevelChanged(result.CurrentLevel);

            if (result.LevelUpCount > 0)
            {
                PublishLevelUp();
            }

            if (result.ReachedMaxLevel)
            {
                PublishGameClear();
            }
        }

        public void PublishGameStateChanged(GameState state)
        {
            GameStateChanged?.Invoke(state);

            if (state == GameState.GameOver)
            {
                PublishGameOver();
            }
            else if (state == GameState.Clear)
            {
                PublishGameClear();
            }
        }

        public void PublishExpChanged(int currentExp)
        {
            ExpChanged?.Invoke(currentExp);
        }

        public void PublishLevelChanged(int newLevel)
        {
            LevelChanged?.Invoke(newLevel);
        }

        public void PublishEatSuccess(int expGained)
        {
            EatSuccess?.Invoke(expGained);
        }

        public void PublishGameOver()
        {
            GameOver?.Invoke();
        }

        public void PublishGameClear()
        {
            GameClear?.Invoke();
        }

        public void PublishStageCleared()
        {
            StageCleared?.Invoke();
        }

        public void PublishLevelUp()
        {
            LevelUp?.Invoke();
        }
    }
}
