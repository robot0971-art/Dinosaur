using System.Collections.Generic;
using DinoGrow.Gameplay.VFX;
using DinoGrow.Infrastructure.Pooling;
using UnityEngine;

namespace DinoGrow.Gameplay.Items
{
    public sealed class HeartDropSpawnService
    {
        public bool TryDrop(
            HeartDropSpawnSettings settings,
            HeartDropSpawnContext context,
            Vector3 spawnPosition,
            Vector3 landingOrigin,
            Vector3 awayDirection,
            out Vector3 landingPosition)
        {
            landingPosition = Vector3.zero;
            if (settings.HeartDropPrefab == null
                || settings.HeartDropChance <= 0f
                || context.Random01() > settings.HeartDropChance)
            {
                return false;
            }

            landingPosition = GetLandingPosition(settings, context, landingOrigin, awayDirection);
            SpawnHeartDrop(settings, context, spawnPosition, landingPosition);
            return true;
        }

        public void ClearDrops(
            IReadOnlyList<Transform> heartDrops,
            IObjectPoolService poolService)
        {
            for (var i = heartDrops.Count - 1; i >= 0; i--)
            {
                var heartDrop = heartDrops[i];
                if (heartDrop == null)
                {
                    continue;
                }

                if (poolService != null)
                {
                    poolService.Despawn(heartDrop);
                    continue;
                }

                Object.Destroy(heartDrop.gameObject);
            }
        }

        private void SpawnHeartDrop(
            HeartDropSpawnSettings settings,
            HeartDropSpawnContext context,
            Vector3 spawnPosition,
            Vector3 landingPosition)
        {
            TrimHeartDropsToLimit(settings, context);
            if (settings.MaxSpawnedHeartDrops == 0)
            {
                return;
            }

            var idleEffectPrefab = settings.EnableIdleEffects
                ? settings.HeartDropIdleEffectPrefab
                : null;

            Transform heartDrop;
            if (context.PoolService != null)
            {
                heartDrop = context.PoolService.Spawn(
                    settings.HeartDropPrefab.transform,
                    landingPosition,
                    Quaternion.identity,
                    context.SpawnParent);
            }
            else
            {
                heartDrop = Object.Instantiate(
                    settings.HeartDropPrefab,
                    landingPosition,
                    Quaternion.identity,
                    context.SpawnParent).transform;
            }

            if (heartDrop == null)
            {
                return;
            }

            SpawnHeartDropEffect(settings, context, landingPosition);
            ConfigurePickup(settings, context, heartDrop);
            ConfigureMotion(settings, heartDrop, landingPosition, idleEffectPrefab);

            if (!context.SpawnedHeartDrops.Contains(heartDrop))
            {
                context.SpawnedHeartDrops.Add(heartDrop);
            }
        }

        private static void ConfigurePickup(
            HeartDropSpawnSettings settings,
            HeartDropSpawnContext context,
            Transform heartDrop)
        {
            if (!heartDrop.TryGetComponent(out HeartPickup pickup))
            {
                pickup = heartDrop.gameObject.AddComponent<HeartPickup>();
            }

            pickup.ConfigurePickupFeedback(
                context.PoolService,
                settings.HeartPickupEffectPrefab,
                settings.HeartPickupSoundClip,
                settings.HeartPickupSoundVolume);
        }

        private static void ConfigureMotion(
            HeartDropSpawnSettings settings,
            Transform heartDrop,
            Vector3 landingPosition,
            GameObject idleEffectPrefab)
        {
            if (!heartDrop.TryGetComponent(out HeartDropMotion motion))
            {
                return;
            }

            motion.ConfigureIdleEffect(settings.EnableIdleEffects ? idleEffectPrefab : null);
            motion.PopTo(landingPosition);
        }

        private void SpawnHeartDropEffect(
            HeartDropSpawnSettings settings,
            HeartDropSpawnContext context,
            Vector3 position)
        {
            if (!settings.EnableSpawnEffect)
            {
                return;
            }

            var effectPrefab = settings.HeartDropIdleEffectPrefab;
            if (effectPrefab == null || effectPrefab.transform == null)
            {
                return;
            }

            if (context.PoolService == null)
            {
                var effect = Object.Instantiate(effectPrefab, position, Quaternion.identity);
                var returner = effect.GetComponent<PooledOneShotVfx>() ?? effect.AddComponent<PooledOneShotVfx>();
                returner.Play(null, effect.transform);
                return;
            }

            var effectRoot = context.PoolService.Spawn(
                effectPrefab.transform,
                position,
                Quaternion.identity,
                context.SpawnParent);
            if (effectRoot == null)
            {
                return;
            }

            var pooledVfx = effectRoot.GetComponent<PooledOneShotVfx>()
                ?? effectRoot.gameObject.AddComponent<PooledOneShotVfx>();
            pooledVfx.Play(context.PoolService, effectRoot);
        }

        private static void TrimHeartDropsToLimit(
            HeartDropSpawnSettings settings,
            HeartDropSpawnContext context)
        {
            context.SpawnedHeartDrops.RemoveAll(drop => drop == null || !drop.gameObject.activeInHierarchy);
            var limit = Mathf.Max(0, settings.MaxSpawnedHeartDrops);
            while (context.SpawnedHeartDrops.Count >= limit && context.SpawnedHeartDrops.Count > 0)
            {
                var oldestDrop = context.SpawnedHeartDrops[0];
                context.SpawnedHeartDrops.RemoveAt(0);
                if (oldestDrop == null)
                {
                    continue;
                }

                if (context.PoolService != null)
                {
                    context.PoolService.Despawn(oldestDrop);
                }
                else
                {
                    Object.Destroy(oldestDrop.gameObject);
                }
            }
        }

        private static Vector3 GetLandingPosition(
            HeartDropSpawnSettings settings,
            HeartDropSpawnContext context,
            Vector3 landingOrigin,
            Vector3 awayDirection)
        {
            awayDirection.y = 0f;
            if (awayDirection.sqrMagnitude < 0.001f)
            {
                awayDirection = context.RandomInsideUnitSphere();
                awayDirection.y = 0f;
            }

            if (awayDirection.sqrMagnitude < 0.001f)
            {
                awayDirection = Vector3.forward;
            }

            var groundedOrigin = context.SnapToGround(landingOrigin);
            groundedOrigin.y += settings.HeightOffset;
            var landingPosition = groundedOrigin
                + awayDirection.normalized * Mathf.Max(0f, settings.KnockbackDistance);
            landingPosition = context.ClampToSpawnArea(landingPosition);
            landingPosition = context.SnapToGround(landingPosition);
            landingPosition.y += settings.HeightOffset;
            return landingPosition;
        }
    }

    public readonly struct HeartDropSpawnSettings
    {
        public HeartDropSpawnSettings(
            GameObject heartDropPrefab,
            GameObject heartDropIdleEffectPrefab,
            bool enableSpawnEffect,
            GameObject heartPickupEffectPrefab,
            AudioClip heartPickupSoundClip,
            float heartPickupSoundVolume,
            float heartDropChance,
            int maxSpawnedHeartDrops,
            bool enableIdleEffects,
            float heightOffset,
            float knockbackDistance)
        {
            HeartDropPrefab = heartDropPrefab;
            HeartDropIdleEffectPrefab = heartDropIdleEffectPrefab;
            EnableSpawnEffect = enableSpawnEffect;
            HeartPickupEffectPrefab = heartPickupEffectPrefab;
            HeartPickupSoundClip = heartPickupSoundClip;
            HeartPickupSoundVolume = Mathf.Clamp01(heartPickupSoundVolume);
            HeartDropChance = Mathf.Clamp01(heartDropChance);
            MaxSpawnedHeartDrops = Mathf.Max(0, maxSpawnedHeartDrops);
            EnableIdleEffects = enableIdleEffects;
            HeightOffset = Mathf.Max(0f, heightOffset);
            KnockbackDistance = Mathf.Max(0f, knockbackDistance);
        }

        public GameObject HeartDropPrefab { get; }
        public GameObject HeartDropIdleEffectPrefab { get; }
        public bool EnableSpawnEffect { get; }
        public GameObject HeartPickupEffectPrefab { get; }
        public AudioClip HeartPickupSoundClip { get; }
        public float HeartPickupSoundVolume { get; }
        public float HeartDropChance { get; }
        public int MaxSpawnedHeartDrops { get; }
        public bool EnableIdleEffects { get; }
        public float HeightOffset { get; }
        public float KnockbackDistance { get; }
    }

    public readonly struct HeartDropSpawnContext
    {
        public HeartDropSpawnContext(
            Transform spawnParent,
            List<Transform> spawnedHeartDrops,
            IObjectPoolService poolService,
            System.Func<float> random01,
            System.Func<Vector3> randomInsideUnitSphere,
            System.Func<Vector3, Vector3> snapToGround,
            System.Func<Vector3, Vector3> clampToSpawnArea)
        {
            SpawnParent = spawnParent;
            SpawnedHeartDrops = spawnedHeartDrops;
            PoolService = poolService;
            Random01 = random01;
            RandomInsideUnitSphere = randomInsideUnitSphere;
            SnapToGround = snapToGround;
            ClampToSpawnArea = clampToSpawnArea;
        }

        public Transform SpawnParent { get; }
        public List<Transform> SpawnedHeartDrops { get; }
        public IObjectPoolService PoolService { get; }
        public System.Func<float> Random01 { get; }
        public System.Func<Vector3> RandomInsideUnitSphere { get; }
        public System.Func<Vector3, Vector3> SnapToGround { get; }
        public System.Func<Vector3, Vector3> ClampToSpawnArea { get; }
    }
}
