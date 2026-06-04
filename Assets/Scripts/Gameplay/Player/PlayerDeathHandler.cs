using DinoGrow.Core.Stage;
using DinoGrow.Gameplay.Animation;
using DinoGrow.Infrastructure.Events;
using UnityEngine;

namespace DinoGrow.Gameplay.Player
{
    public sealed class PlayerDeathHandler
    {
        private readonly GameStateController gameState;
        private readonly DeathEffectService deathEffectService;
        private readonly GameEventBus eventBus;
        private readonly PlayerMovementMotor movementMotor;
        private readonly PlayerStepSoundController stepSoundController;
        private readonly DinoAnimatorView animatorView;

        public PlayerDeathHandler(
            GameStateController gameState,
            DeathEffectService deathEffectService,
            GameEventBus eventBus,
            PlayerMovementMotor movementMotor,
            PlayerStepSoundController stepSoundController,
            DinoAnimatorView animatorView)
        {
            this.gameState = gameState;
            this.deathEffectService = deathEffectService;
            this.eventBus = eventBus;
            this.movementMotor = movementMotor;
            this.stepSoundController = stepSoundController;
            this.animatorView = animatorView;
        }

        public bool TriggerGameOver(bool isDead, Vector3 bloodEffectPosition, System.Action onBeforeDeadAnimation)
        {
            if (isDead)
            {
                return true;
            }

            gameState.GameOver();
            onBeforeDeadAnimation?.Invoke();
            movementMotor?.StopBody();
            animatorView?.SetMove(0f, false);
            stepSoundController?.Stop();
            deathEffectService?.SpawnBlood(bloodEffectPosition);
            animatorView?.SetDead(true);
            eventBus?.PublishGameStateChanged(gameState.State);
            return true;
        }
    }
}
