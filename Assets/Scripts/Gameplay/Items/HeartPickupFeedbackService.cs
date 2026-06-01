using DinoGrow.Gameplay.VFX;
using DinoGrow.Infrastructure.Pooling;
using UnityEngine;

namespace DinoGrow.Gameplay.Items
{
    public sealed class HeartPickupFeedbackService
    {
        private const float SoundSpatialBlend = 0.85f;
        private const float SoundMinDistance = 1.5f;
        private const float SoundMaxDistance = 18f;

        public void Play(
            Vector3 position,
            IObjectPoolService poolService,
            GameObject pickupEffectPrefab,
            AudioClip pickupSoundClip,
            float pickupSoundVolume)
        {
            PlayPickupSound(position, pickupSoundClip, pickupSoundVolume);
            SpawnPickupEffect(position, poolService, pickupEffectPrefab);
        }

        private static void SpawnPickupEffect(
            Vector3 position,
            IObjectPoolService poolService,
            GameObject pickupEffectPrefab)
        {
            if (pickupEffectPrefab == null || pickupEffectPrefab.transform == null)
            {
                return;
            }

            if (poolService == null)
            {
                var effect = Object.Instantiate(pickupEffectPrefab, position, Quaternion.identity);
                var returner = effect.GetComponent<PooledOneShotVfx>() ?? effect.AddComponent<PooledOneShotVfx>();
                returner.Play(null, effect.transform);
                return;
            }

            var effectRoot = poolService.Spawn(pickupEffectPrefab.transform, position, Quaternion.identity);
            if (effectRoot == null)
            {
                return;
            }

            var pooledVfx = effectRoot.GetComponent<PooledOneShotVfx>()
                ?? effectRoot.gameObject.AddComponent<PooledOneShotVfx>();
            pooledVfx.Play(poolService, effectRoot);
        }

        private static void PlayPickupSound(
            Vector3 position,
            AudioClip pickupSoundClip,
            float pickupSoundVolume)
        {
            if (pickupSoundClip == null || pickupSoundVolume <= 0f)
            {
                return;
            }

            var soundObject = new GameObject("HeartPickupSound");
            soundObject.transform.position = position;

            var source = soundObject.AddComponent<AudioSource>();
            source.clip = pickupSoundClip;
            source.volume = Mathf.Clamp01(pickupSoundVolume);
            source.spatialBlend = SoundSpatialBlend;
            source.minDistance = SoundMinDistance;
            source.maxDistance = SoundMaxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();

            Object.Destroy(soundObject, pickupSoundClip.length + 0.1f);
        }
    }
}
