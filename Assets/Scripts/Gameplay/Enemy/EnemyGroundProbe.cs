using DinoGrow.Gameplay.Player;
using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class EnemyGroundProbe
    {
        private readonly Transform ownerTransform;
        private LayerMask groundLayers;
        private Vector3 areaCenter;
        private float groundRaycastHeight;
        private float groundRaycastDistance;
        private float groundOffset;
        private float maxGroundSnapStep;

        public EnemyGroundProbe(Transform ownerTransform)
        {
            this.ownerTransform = ownerTransform;
        }

        public float VisualBottomOffset { get; private set; }

        public void Configure(
            LayerMask groundLayers,
            Vector3 areaCenter,
            float groundRaycastHeight,
            float groundRaycastDistance,
            float groundOffset,
            float maxGroundSnapStep)
        {
            this.groundLayers = groundLayers;
            this.areaCenter = areaCenter;
            this.groundRaycastHeight = groundRaycastHeight;
            this.groundRaycastDistance = groundRaycastDistance;
            this.groundOffset = groundOffset;
            this.maxGroundSnapStep = maxGroundSnapStep;
        }

        public Vector3 SnapImmediate(Vector3 position)
        {
            if (TryGetGroundY(position, out var targetY))
            {
                position.y = targetY - VisualBottomOffset;
            }

            return position;
        }

        public Vector3 Snap(Vector3 position)
        {
            if (TryGetGroundY(position, out var targetY))
            {
                position.y = Mathf.MoveTowards(position.y, targetY - VisualBottomOffset, maxGroundSnapStep);
            }

            return position;
        }

        public void CacheVisualBottomOffset()
        {
            var bounds = CalculateWorldVisualBounds();
            VisualBottomOffset = bounds.HasValue
                ? bounds.Value.min.y - ownerTransform.position.y
                : 0f;
        }

        public bool IsWaterAt(Vector3 position)
        {
            var originY = Mathf.Max(position.y + groundRaycastHeight, areaCenter.y + groundRaycastHeight);
            var origin = new Vector3(position.x, originY, position.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, groundRaycastDistance, groundLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                return false;
            }

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<DinoEnemy>() != null)
                {
                    continue;
                }

                if (hit.collider.GetComponentInParent<PlayerDinoController>() != null)
                {
                    continue;
                }

                return IsWaterCollider(hit.collider);
            }

            return false;
        }

        private bool TryGetGroundY(Vector3 position, out float groundY)
        {
            groundY = position.y;
            var originY = Mathf.Max(position.y + groundRaycastHeight, areaCenter.y + groundRaycastHeight);
            var origin = new Vector3(position.x, originY, position.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, groundRaycastDistance, groundLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                return false;
            }

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
            {
                if (!IsGroundCollider(hit.collider))
                {
                    continue;
                }

                if (hit.collider.GetComponentInParent<DinoEnemy>() != null)
                {
                    continue;
                }

                if (hit.collider.GetComponentInParent<PlayerDinoController>() != null)
                {
                    continue;
                }

                groundY = hit.point.y + groundOffset;
                return true;
            }

            return false;
        }

        private Bounds? CalculateWorldVisualBounds()
        {
            var renderers = ownerTransform.GetComponentsInChildren<Renderer>();
            var hasBounds = false;
            var bounds = new Bounds(ownerTransform.position, Vector3.zero);

            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer.GetComponent<TextMesh>() != null)
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

            return hasBounds ? bounds : null;
        }

        private static bool IsGroundCollider(Collider targetCollider)
        {
            if (targetCollider == null)
            {
                return false;
            }

            var target = targetCollider.transform;
            while (target != null)
            {
                if (IsNonGroundSurfaceName(target.name))
                {
                    return false;
                }

                target = target.parent;
            }

            return true;
        }

        private static bool IsWaterCollider(Collider targetCollider)
        {
            if (targetCollider == null)
            {
                return false;
            }

            var target = targetCollider.transform;
            while (target != null)
            {
                if (target.name == "Water")
                {
                    return true;
                }

                target = target.parent;
            }

            return false;
        }

        private static bool IsNonGroundSurfaceName(string targetName)
        {
            return targetName == "Water"
                || targetName == "MapBoundary"
                || targetName.StartsWith("Tree_", System.StringComparison.Ordinal)
                || targetName.StartsWith("Rock_", System.StringComparison.Ordinal)
                || targetName.StartsWith("SnowRock_", System.StringComparison.Ordinal)
                || targetName.StartsWith("SnowTree_", System.StringComparison.Ordinal)
                || targetName.Contains("Tree", System.StringComparison.OrdinalIgnoreCase)
                || targetName.Contains("Rock", System.StringComparison.OrdinalIgnoreCase)
                || targetName.Contains("Cactus", System.StringComparison.OrdinalIgnoreCase)
                || targetName.Contains("Boulder", System.StringComparison.OrdinalIgnoreCase)
                || targetName.Contains("Stone", System.StringComparison.OrdinalIgnoreCase)
                || targetName.Contains("Cliff", System.StringComparison.OrdinalIgnoreCase)
                || targetName.Contains("Stump", System.StringComparison.OrdinalIgnoreCase)
                || targetName.Contains("Log", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
