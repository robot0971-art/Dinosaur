using DinoGrow.Gameplay;
using DinoGrow.Gameplay.Enemy;
using UnityEngine;

namespace DinoGrow.Gameplay.Player
{
    public sealed class PlayerHitHandler
    {
        private readonly GameObject owner;
        private readonly DeathEffectService deathEffectService;
        private readonly AudioClip hitSoundClip;
        private readonly float hitSoundVolume;
        private AudioSource hitSoundSource;

        public PlayerHitHandler(
            GameObject owner,
            DeathEffectService deathEffectService,
            AudioClip hitSoundClip,
            AudioSource hitSoundSource,
            float hitSoundVolume)
        {
            this.owner = owner;
            this.deathEffectService = deathEffectService;
            this.hitSoundClip = hitSoundClip;
            this.hitSoundSource = hitSoundSource;
            this.hitSoundVolume = hitSoundVolume;
        }

        public void TakeHit(DinoEnemy attacker, GameHudHeartUI heartUI, Vector3 hitEffectPosition, System.Action<Vector3> triggerGameOver)
        {
            if (attacker == null)
            {
                triggerGameOver?.Invoke(hitEffectPosition);
                return;
            }

            attacker.OnPlayerBitten();
            if (heartUI == null || !heartUI.TryRemoveHeart())
            {
                PlayHitSound();
                triggerGameOver?.Invoke(hitEffectPosition);
                return;
            }

            deathEffectService?.SpawnBlood(hitEffectPosition);
            PlayHitSound();
        }

        private void PlayHitSound()
        {
            if (hitSoundClip == null || owner == null)
            {
                return;
            }

            if (hitSoundSource == null)
            {
                hitSoundSource = owner.AddComponent<AudioSource>();
            }

            hitSoundSource.playOnAwake = false;
            hitSoundSource.loop = false;
            hitSoundSource.spatialBlend = 0f;
            hitSoundSource.volume = hitSoundVolume;
            hitSoundSource.PlayOneShot(hitSoundClip, hitSoundVolume);
        }
    }
}
