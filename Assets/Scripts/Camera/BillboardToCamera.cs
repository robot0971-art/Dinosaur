using UnityEngine;

namespace DinoGrow.Camera
{
    public sealed class BillboardToCamera : MonoBehaviour
    {
        [SerializeField] private bool yawOnly = true;
        [SerializeField] private bool invertForward = true;
        [SerializeField] private float rotationOffsetY;

        private Transform target;

        public void SetTarget(Transform targetTransform)
        {
            target = targetTransform;
            FaceTarget();
        }

        private void LateUpdate()
        {
            FaceTarget();
        }

        private void FaceTarget()
        {
            if (target == null)
            {
                return;
            }

            var direction = transform.position - target.position;
            if (!invertForward)
            {
                direction = -direction;
            }

            if (yawOnly)
            {
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var rotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = rotation * Quaternion.Euler(0f, rotationOffsetY, 0f);
        }
    }
}
