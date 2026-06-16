using DinoGrow.Gameplay.Player;
using DinoGrow.Gameplay.VFX;
using DinoGrow.Infrastructure.Pooling;
using System.Collections;
using UnityEngine;

namespace DinoGrow.Gameplay.Items
{
    public sealed class HeartPickup : MonoBehaviour
    {
        [SerializeField] private float pickupRadius = 1.2f;
        [SerializeField] private float pickupHeight = 2.5f;
        [SerializeField] private float pickupDelay = 0.45f;
        [SerializeField] private float nearbyCheckInterval = 0.12f;
        [SerializeField] private float lifetime = 30f;
        [SerializeField] private GameObject pickupEffectPrefab;
        [SerializeField] private AudioClip pickupSoundClip;
        [SerializeField, Range(0f, 1f)] private float pickupSoundVolume = 1f;

        private static readonly Collider[] NearbyColliders = new Collider[12];
        private bool consumed;
        private float pickupEnabledTime;
        private float nextNearbyCheckTime;
        private IObjectPoolService poolService;
        private HeartDropMotion dropMotion;
        private Coroutine lifetimeRoutine;

        public void ConfigurePickupEffect(IObjectPoolService pool, GameObject effectPrefab)
        {
            poolService = pool;
            pickupEffectPrefab = effectPrefab;
        }

        public void ConfigurePickupFeedback(
            IObjectPoolService pool,
            GameObject effectPrefab,
            AudioClip soundClip,
            float soundVolume)
        {
            poolService = pool;
            pickupEffectPrefab = effectPrefab;
            pickupSoundClip = soundClip;
            pickupSoundVolume = Mathf.Clamp01(soundVolume);
        }

        private void Awake()
        {
            dropMotion = GetComponent<HeartDropMotion>();
            EnsureTriggerCollider();
        }

        private void OnEnable()
        {
            consumed = false;
            if (dropMotion == null)
            {
                dropMotion = GetComponent<HeartDropMotion>();
            }

            pickupEnabledTime = Time.time + Mathf.Max(0f, pickupDelay);
            nextNearbyCheckTime = pickupEnabledTime;
            StartLifetimeTimer();
        }

        private void OnDisable()
        {
            StopLifetimeTimer();
        }

        private void OnTriggerEnter(Collider other)
        {
            TryConsume(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryConsume(other);
        }

        private void FixedUpdate()
        {
            TryConsumeNearbyPlayer();
        }

        private void TryConsumeNearbyPlayer()
        {
            if (consumed)
            {
                return;
            }

            if (Time.time < pickupEnabledTime)
            {
                return;
            }

            if (!CanPickupAfterLanding())
            {
                return;
            }

            if (Time.time < nextNearbyCheckTime)
            {
                return;
            }

            nextNearbyCheckTime = Time.time + Mathf.Max(0.02f, nearbyCheckInterval);
            var center = transform.position + Vector3.up * (pickupHeight * 0.5f);
            var colliderCount = Physics.OverlapSphereNonAlloc(
                center,
                pickupRadius,
                NearbyColliders,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);

            for (var i = 0; i < colliderCount; i++)
            {
                if (TryConsume(NearbyColliders[i]))
                {
                    return;
                }
            }
        }

        private bool TryConsume(Collider other)
        {
            if (consumed || other == null)
            {
                return false;
            }

            if (Time.time < pickupEnabledTime)
            {
                return false;
            }

            if (!CanPickupAfterLanding())
            {
                return false;
            }

            var player = other.GetComponentInParent<PlayerDinoController>();
            if (player == null || !player.TryAddHeart())
            {
                return false;
            }

            SpawnPickupEffect();
            consumed = true;
            ReturnToPool();
            return true;
        }

        private bool CanPickupAfterLanding()
        {
            return dropMotion == null || dropMotion.IsSettled;
        }

        private void StartLifetimeTimer()
        {
            StopLifetimeTimer();
            var delay = Mathf.Max(0f, lifetime);
            if (delay <= 0f)
            {
                return;
            }

            lifetimeRoutine = StartCoroutine(ReturnAfterLifetime(delay));
        }

        private void StopLifetimeTimer()
        {
            if (lifetimeRoutine == null)
            {
                return;
            }

            StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = null;
        }

        private IEnumerator ReturnAfterLifetime(float delay)
        {
            yield return new WaitForSeconds(delay);
            lifetimeRoutine = null;
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            StopLifetimeTimer();
            if (poolService != null)
            {
                poolService.Despawn(transform);
                return;
            }

            gameObject.SetActive(false);
        }

        private void SpawnPickupEffect()
        {
            PlayPickupSound();

            if (pickupEffectPrefab == null)
            {
                return;
            }

            var prefabTransform = pickupEffectPrefab != null ? pickupEffectPrefab.transform : null;
            if (prefabTransform == null)
            {
                return;
            }

            if (poolService == null)
            {
                var effect = Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
                var returner = effect.GetComponent<PooledOneShotVfx>() ?? effect.AddComponent<PooledOneShotVfx>();
                returner.Play(null, effect.transform);
                return;
            }

            var effectRoot = poolService.Spawn(prefabTransform, transform.position, Quaternion.identity);
            if (effectRoot == null)
            {
                return;
            }

            var pooledVfx = effectRoot.GetComponent<PooledOneShotVfx>() ?? effectRoot.gameObject.AddComponent<PooledOneShotVfx>();
            pooledVfx.Play(poolService, effectRoot);
        }

        private void PlayPickupSound()
        {
            if (pickupSoundClip == null || pickupSoundVolume <= 0f)
            {
                return;
            }

            var soundObject = new GameObject("HeartPickupSound");
            soundObject.transform.position = transform.position;

            var source = soundObject.AddComponent<AudioSource>();
            source.clip = pickupSoundClip;
            source.volume = pickupSoundVolume;
            source.spatialBlend = 0.85f;
            source.minDistance = 1.5f;
            source.maxDistance = 18f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();

            Destroy(soundObject, pickupSoundClip.length + 0.1f);
        }

        private void EnsureTriggerCollider()
        {
            var targetCollider = GetComponent<Collider>();
            if (targetCollider == null)
            {
                var sphere = gameObject.AddComponent<SphereCollider>();
                sphere.radius = pickupRadius;
                sphere.center = Vector3.up * (pickupHeight * 0.5f);
                targetCollider = sphere;
            }

            targetCollider.isTrigger = true;
            EnsureRigidbody();
        }

        private void EnsureRigidbody()
        {
            if (!TryGetComponent(out Rigidbody body))
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.useGravity = false;
            body.isKinematic = true;
        }
    }
}
