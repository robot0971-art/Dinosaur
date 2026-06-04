using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;
using DinoGrow.Gameplay.Enemy;
using DinoGrow.Infrastructure.Events;
using UnityEngine;

namespace DinoGrow.Gameplay.Player
{
    public sealed class PlayerEatHandler
    {
        private readonly GrowthSystem growthSystem;
        private readonly PlayerProgress progress;
        private readonly StageRule stageRule;
        private readonly GameStateController gameState;
        private readonly DeathEffectService deathEffectService;
        private readonly EatingSoundService eatingSoundService;
        private readonly GameEventBus eventBus;

        public PlayerEatHandler(
            GrowthSystem growthSystem,
            PlayerProgress progress,
            StageRule stageRule,
            GameStateController gameState,
            DeathEffectService deathEffectService,
            EatingSoundService eatingSoundService,
            GameEventBus eventBus)
        {
            this.growthSystem = growthSystem;
            this.progress = progress;
            this.stageRule = stageRule;
            this.gameState = gameState;
            this.deathEffectService = deathEffectService;
            this.eatingSoundService = eatingSoundService;
            this.eventBus = eventBus;
        }

        public void Eat(DinoEnemy enemy, Vector3 eatEffectPosition, System.Action<GrowthResult> onGrowthChanged)
        {
            if (enemy == null || growthSystem == null || progress == null)
            {
                return;
            }

            var enemyLevel = enemy.Level;
            enemy.Eaten();
            deathEffectService?.SpawnBlood(eatEffectPosition);
            eatingSoundService?.PlayAt(eatEffectPosition);

            var growthResult = growthSystem.AddEnemyExp(progress, enemyLevel);
            onGrowthChanged?.Invoke(growthResult);

            eventBus?.PublishEnemyEaten(enemyLevel, growthResult.GainedExp);
            eventBus?.PublishPlayerGrowthChanged(growthResult);

            if (stageRule != null && gameState != null && stageRule.IsClearLevel(progress.Level))
            {
                gameState.Clear();
                eventBus?.PublishGameStateChanged(gameState.State);
            }
        }
    }
}
