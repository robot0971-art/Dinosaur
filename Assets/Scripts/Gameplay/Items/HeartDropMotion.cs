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

        private Vector3 startPosition;
        private float elapsed;
        private bool settled;

        private void OnEnable()
        {
            AlignBottomToSpawnHeight();
            startPosition = transform.position;
            elapsed = 0f;
            settled = false;
        }

        private void Update()
        {
            Rotate();

            if (settled)
            {
                return;
            }

            elapsed += Time.deltaTime;
            var duration = Mathf.Max(0.01f, popDuration);
            var progress = Mathf.Clamp01(elapsed / duration);
            var heightOffset = Mathf.Sin(progress * Mathf.PI) * popHeight;
            transform.position = startPosition + Vector3.up * heightOffset;

            if (progress >= 1f)
            {
                transform.position = startPosition;
                settled = true;
            }
        }

        private void Rotate()
        {
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
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
