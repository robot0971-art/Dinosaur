using UnityEngine;

namespace DinoGrow.Gameplay.Items
{
    public sealed class HeartDropMotion : MonoBehaviour
    {
        [Header("Ground Alignment")]
        [Tooltip("Keeps the bottom of the visible heart this far above the spawn height.")]
        [Min(0f)]
        [SerializeField] private float groundClearance = 0.05f;

        [Header("Drop Motion")]
        [Tooltip("How high the heart pops up after it spawns.")]
        [Min(0f)]
        [SerializeField] private float popHeight = 1.2f;

        [Tooltip("How long it takes for the heart to pop up and settle back down.")]
        [Min(0.01f)]
        [SerializeField] private float popDuration = 0.55f;

        [Header("Idle Motion")]
        [Tooltip("Y-axis rotation speed in degrees per second.")]
        [Min(0f)]
        [SerializeField] private float rotationSpeed = 180f;
        [SerializeField] private GameObject idleEffectPrefab;
        [SerializeField] private Vector3 idleEffectOffset = Vector3.zero;
        [SerializeField, Min(0f)] private float idleEffectLifetime = 15f;

        private Vector3 startPosition;
        private Vector3 landingPosition;
        private float elapsed;
        private bool settled;
        private Transform idleEffectInstance;
        private float idleEffectStopTime;

        public bool IsSettled => settled;

        public void ConfigureIdleEffect(GameObject effectPrefab)
        {
            idleEffectPrefab = effectPrefab;
            if (idleEffectPrefab == null)
            {
                DisableIdleEffect();
                return;
            }

            EnsureIdleEffect();
        }

        public void PopTo(Vector3 targetPosition)
        {
            startPosition = transform.position;
            landingPosition = targetPosition;
            elapsed = 0f;
            settled = false;
        }

        private void OnEnable()
        {
            AlignBottomToSpawnHeight();
            startPosition = transform.position;
            landingPosition = startPosition;
            elapsed = 0f;
            settled = false;
            EnsureIdleEffect();
        }

        private void OnDisable()
        {
            DisableIdleEffect();
        }

        private void Update()
        {
            Rotate();
            UpdateIdleEffectLifetime();

            if (settled)
            {
                return;
            }

            elapsed += Time.deltaTime;
            var duration = Mathf.Max(0.01f, popDuration);
            var progress = Mathf.Clamp01(elapsed / duration);
            var heightOffset = Mathf.Sin(progress * Mathf.PI) * popHeight;
            transform.position = Vector3.Lerp(startPosition, landingPosition, progress) + Vector3.up * heightOffset;

            if (progress >= 1f)
            {
                transform.position = landingPosition;
                settled = true;
            }
        }

        private void Rotate()
        {
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
        }

        private void EnsureIdleEffect()
        {
            if (idleEffectPrefab == null)
            {
                return;
            }

            idleEffectStopTime = idleEffectLifetime > 0f
                ? Time.time + idleEffectLifetime
                : 0f;

            if (idleEffectInstance != null)
            {
                idleEffectInstance.gameObject.SetActive(true);
                foreach (var particle in idleEffectInstance.GetComponentsInChildren<ParticleSystem>(true))
                {
                    particle.Play(true);
                }

                return;
            }

            var prefabTransform = idleEffectPrefab.transform;
            if (prefabTransform == null)
            {
                return;
            }

            idleEffectInstance = Instantiate(idleEffectPrefab, transform).transform;

            if (idleEffectInstance == null)
            {
                return;
            }

            idleEffectInstance.transform.localPosition = idleEffectOffset;
            idleEffectInstance.transform.localRotation = Quaternion.identity;
            idleEffectInstance.transform.localScale = Vector3.one;

            foreach (var particle in idleEffectInstance.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                main.loop = true;
                particle.Play(true);
            }
        }

        private void UpdateIdleEffectLifetime()
        {
            if (idleEffectInstance == null || idleEffectStopTime <= 0f || Time.time < idleEffectStopTime)
            {
                return;
            }

            idleEffectStopTime = 0f;
            DisableIdleEffect();
        }

        private void DisableIdleEffect()
        {
            idleEffectStopTime = 0f;
            if (idleEffectInstance == null)
            {
                return;
            }

            foreach (var particle in idleEffectInstance.GetComponentsInChildren<ParticleSystem>(true))
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            idleEffectInstance.gameObject.SetActive(false);
        }

        private void AlignBottomToSpawnHeight()
        {
            if (!TryGetVisibleBounds(out var bounds))
            {
                return;
            }

            var targetBottomY = transform.position.y + groundClearance;
            var lift = targetBottomY - bounds.min.y;
            if (lift > 0f)
            {
                transform.position += Vector3.up * lift;
            }
        }

        private bool TryGetVisibleBounds(out Bounds bounds)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            bounds = new Bounds(transform.position, Vector3.zero);
            var hasBounds = false;

            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer == null || !targetRenderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = targetRenderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(targetRenderer.bounds);
            }

            return hasBounds;
        }
    }
}
