using DinoGrow.Gameplay.Enemy;
using UnityEngine;

namespace DinoGrow.Gameplay.Player
{
    internal static class PlayerMovementUtility
    {
        public static bool TryGetCameraRelativeDirection(
            Vector2 input,
            Transform cameraTransform,
            out Vector3 direction)
        {
            direction = Vector3.zero;
            if (input.sqrMagnitude <= 0.001f || cameraTransform == null)
            {
                return false;
            }

            var cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            var cameraRight = cameraTransform.right;
            cameraRight.y = 0f;

            if (cameraForward.sqrMagnitude <= 0.001f || cameraRight.sqrMagnitude <= 0.001f)
            {
                return false;
            }

            direction = cameraForward.normalized * input.y + cameraRight.normalized * input.x;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return false;
            }

            direction.Normalize();
            return true;
        }

        public static Vector3 ClampToBounds(
            Vector3 position,
            bool useBounds,
            Vector3 boundsCenter,
            Vector2 boundsSize)
        {
            if (!useBounds)
            {
                return position;
            }

            var halfSize = boundsSize * 0.5f;
            position.x = Mathf.Clamp(
                position.x,
                boundsCenter.x - halfSize.x,
                boundsCenter.x + halfSize.x);
            position.z = Mathf.Clamp(
                position.z,
                boundsCenter.z - halfSize.y,
                boundsCenter.z + halfSize.y);
            return position;
        }

        public static bool IsWaterCollider(Collider targetCollider)
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

        public static bool ShouldIgnoreObstacle(
            Collider targetCollider,
            LayerMask groundLayers)
        {
            if (targetCollider == null || targetCollider.isTrigger)
            {
                return true;
            }

            if (targetCollider.GetComponentInParent<PlayerDinoController>() != null)
            {
                return true;
            }

            if (targetCollider.GetComponentInParent<DinoEnemy>() != null)
            {
                return true;
            }

            if ((groundLayers.value & (1 << targetCollider.gameObject.layer)) != 0)
            {
                return true;
            }

            return IsWaterCollider(targetCollider);
        }

        public static void GetObstacleCapsule(
            Vector3 rootPosition,
            float obstacleRadius,
            float obstacleHeight,
            Vector3 obstacleCenterOffset,
            out Vector3 point1,
            out Vector3 point2,
            out float radius)
        {
            radius = Mathf.Max(0.05f, obstacleRadius);
            var height = Mathf.Max(obstacleHeight, radius * 2f);
            var center = rootPosition + obstacleCenterOffset;

            var halfSegment = Mathf.Max(0f, (height * 0.5f) - radius);
            point1 = center + Vector3.up * halfSegment;
            point2 = center - Vector3.up * halfSegment;
        }
    }
}
