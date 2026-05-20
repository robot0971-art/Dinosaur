using UnityEngine;

namespace DinoGrow.Infrastructure.DI
{
    public sealed class CameraReference
    {
        public CameraReference(Transform transform)
        {
            Transform = transform;
        }

        public Transform Transform { get; }
    }
}
