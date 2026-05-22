using UnityEngine;

namespace DinoGrow.Infrastructure.Pooling
{
    public interface IObjectPoolService
    {
        T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            where T : Component;

        void Despawn(Component instance);
    }
}
