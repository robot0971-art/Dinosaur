using DinoGrow.Gameplay;
using DinoGrow.Gameplay.Enemy;
using UnityEngine;

namespace DinoGrow.Gameplay.Player
{
    public sealed class PlayerMovementMotor
    {
        private readonly Rigidbody body;
        private readonly Transform ownerTransform;

        private LayerMask groundLayers;
        private float groundRaycastHeight;
        private float groundRaycastDistance;
        private float groundOffset;
        private float maxGroundSnapStep;
        private LayerMask obstacleLayers;
        private float obstacleSkinWidth;
        private float maxObstacleCorrectionStep;
        private float minObstacleRadius;
        private float minObstacleHeight;

        private float obstacleRadius = 0.45f;
        private float obstacleHeight = 1.8f;
        private Vector3 obstacleCenterOffset = new(0f, 0.9f, 0f);

        public PlayerMovementMotor(Rigidbody body, Transform ownerTransform)
        {
            this.body = body;
            this.ownerTransform = ownerTransform;
        }

        public float VisualBottomOffset { get; private set; }

        public void Configure(
            LayerMask groundLayers,
            float groundRaycastHeight,
            float groundRaycastDistance,
            float groundOffset,
            float maxGroundSnapStep,
            LayerMask obstacleLayers,
            float obstacleSkinWidth,
            float maxObstacleCorrectionStep,
            float minObstacleRadius,
            float minObstacleHeight)
        {
            this.groundLayers = groundLayers;
            this.groundRaycastHeight = groundRaycastHeight;
            this.groundRaycastDistance = groundRaycastDistance;
            this.groundOffset = groundOffset;
            this.maxGroundSnapStep = maxGroundSnapStep;
            this.obstacleLayers = obstacleLayers;
            this.obstacleSkinWidth = obstacleSkinWidth;
            this.maxObstacleCorrectionStep = maxObstacleCorrectionStep;
            this.minObstacleRadius = minObstacleRadius;
            this.minObstacleHeight = minObstacleHeight;
        }

        public void ConfigureBody()
        {
            if (body == null)
            {
                return;
            }

            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            StopBody();
        }

        public void StopBody()
        {
            if (body == null || body.isKinematic)
            {
                return;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        public void MoveBody(
            Vector3 horizontalVelocity,
            bool useMovementBounds,
            Vector3 movementBoundsCenter,
            Vector2 movementBoundsSize)
        {
            if (body == null)
            {
                return;
            }

            var moveDelta = horizontalVelocity * Time.fixedDeltaTime;
            var hasMoveInput = moveDelta.sqrMagnitude > 0.000001f;
            var resolvedMoveDelta = ResolveObstacleMove(moveDelta);
            if (hasMoveInput && resolvedMoveDelta.sqrMagnitude <= 0.000001f)
            {
                resolvedMoveDelta = moveDelta;
            }

            moveDelta = resolvedMoveDelta;
            var position = body.position + moveDelta;
            if (hasMoveInput)
            {
                position += ResolveObstaclePenetration(position);
            }

            position = PlayerMovementUtility.ClampToBounds(position, useMovementBounds, movementBoundsCenter, movementBoundsSize);
            if (TryGetGroundY(position, out var groundY))
            {
                var targetY = groundY - VisualBottomOffset;
                position.y = Mathf.MoveTowards(body.position.y, targetY, maxGroundSnapStep);
            }

            body.MovePosition(position);
            StopBody();
        }

        public bool TrySnapToGround(out Vector3 snappedPosition)
        {
            snappedPosition = body != null ? body.position : Vector3.zero;
            if (body == null || ownerTransform == null || !TryGetGroundY(body.position, out var groundY))
            {
                return false;
            }

            snappedPosition = body.position;
            snappedPosition.y = groundY - VisualBottomOffset;
            ownerTransform.position = snappedPosition;
            body.position = snappedPosition;
            StopBody();
            return true;
        }

        public void CacheVisualBottomOffset()
        {
            var bounds = CalculateWorldVisualBounds();
            VisualBottomOffset = bounds.HasValue
                ? bounds.Value.min.y - ownerTransform.position.y
                : 0f;
        }

        public void CacheObstacleShape()
        {
            var bounds = CalculateWorldVisualBounds();
            if (!bounds.HasValue)
            {
                obstacleRadius = minObstacleRadius;
                obstacleHeight = minObstacleHeight;
                obstacleCenterOffset = Vector3.up * (obstacleHeight * 0.5f);
                return;
            }

            var value = bounds.Value;
            obstacleRadius = Mathf.Max(minObstacleRadius, Mathf.Min(value.size.x, value.size.z) * 0.28f);
            obstacleHeight = Mathf.Max(minObstacleHeight, value.size.y * 0.85f);
            var center = value.center;
            center.y = value.min.y + obstacleHeight * 0.5f;
            obstacleCenterOffset = center - ownerTransform.position;
        }

        private Vector3 ResolveObstaclePenetration(Vector3 rootPosition)
        {
            PlayerMovementUtility.GetObstacleCapsule(
                rootPosition,
                obstacleRadius,
                obstacleHeight,
                obstacleCenterOffset,
                out var point1,
                out var point2,
                out var radius);
            var overlaps = Physics.OverlapCapsule(
                point1,
                point2,
                radius,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);

            var correction = Vector3.zero;
            var capsuleCenter = (point1 + point2) * 0.5f;
            foreach (var overlap in overlaps)
            {
                if (PlayerMovementUtility.ShouldIgnoreObstacle(overlap, groundLayers))
                {
                    continue;
                }

                var closestPoint = overlap.ClosestPoint(capsuleCenter);
                var direction = capsuleCenter - closestPoint;
                direction.y = 0f;
                var distance = direction.magnitude;
                if (distance >= radius)
                {
                    continue;
                }

                if (distance <= 0.000001f)
                {
                    continue;
                }

                correction += direction.normalized * (radius - distance + obstacleSkinWidth);
            }

            correction.y = 0f;
            var maxStep = Mathf.Max(0.01f, maxObstacleCorrectionStep);
            if (correction.sqrMagnitude > maxStep * maxStep)
            {
                correction = correction.normalized * maxStep;
            }

            return correction;
        }

        private Vector3 ResolveObstacleMove(Vector3 moveDelta)
        {
            moveDelta.y = 0f;
            if (moveDelta.sqrMagnitude <= 0.000001f)
            {
                return Vector3.zero;
            }

            if (!IsObstacleInMove(moveDelta, out var hit))
            {
                return moveDelta;
            }

            var slide = Vector3.ProjectOnPlane(moveDelta, hit.normal);
            slide.y = 0f;
            if (slide.sqrMagnitude <= 0.000001f || IsObstacleInMove(slide, out _))
            {
                return Vector3.zero;
            }

            return slide;
        }

        private bool IsObstacleInMove(Vector3 moveDelta, out RaycastHit hit)
        {
            var distance = moveDelta.magnitude;
            if (distance <= 0.0001f || body == null)
            {
                hit = default;
                return false;
            }

            var direction = moveDelta / distance;
            PlayerMovementUtility.GetObstacleCapsule(
                body.position,
                obstacleRadius,
                obstacleHeight,
                obstacleCenterOffset,
                out var point1,
                out var point2,
                out var radius);
            var castDistance = distance + obstacleSkinWidth;
            var hits = Physics.CapsuleCastAll(
                point1,
                point2,
                radius,
                direction,
                castDistance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var candidate in hits)
            {
                if (PlayerMovementUtility.ShouldIgnoreObstacle(candidate.collider, groundLayers))
                {
                    continue;
                }

                hit = candidate;
                return true;
            }

            hit = default;
            return false;
        }

        private bool TryGetGroundY(Vector3 position, out float groundY)
        {
            groundY = position.y;
            var origin = new Vector3(position.x, position.y + groundRaycastHeight, position.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, groundRaycastDistance, groundLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                return false;
            }

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<PlayerDinoController>() != null)
                {
                    continue;
                }

                if (hit.collider.GetComponentInParent<DinoEnemy>() != null)
                {
                    continue;
                }

                if (PlayerMovementUtility.IsWaterCollider(hit.collider))
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
            return ownerTransform != null && RendererBoundsUtility.TryCalculateVisibleBounds(ownerTransform, out var bounds)
                ? bounds
                : null;
        }
    }
}
