using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DinoGrow.Infrastructure.Pooling
{
    public sealed class ObjectPoolService : IObjectPoolService
    {
        private readonly IObjectResolver resolver;
        private readonly Dictionary<Component, Component> prefabByInstance = new();
        private readonly Dictionary<Component, Queue<Component>> poolByPrefab = new();

        public ObjectPoolService(IObjectResolver resolver)
        {
            this.resolver = resolver;
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            where T : Component
        {
            if (prefab == null)
            {
                return null;
            }

            if (!poolByPrefab.TryGetValue(prefab, out var pool))
            {
                pool = new Queue<Component>();
                poolByPrefab[prefab] = pool;
            }

            T instance;
            if (pool.Count > 0)
            {
                instance = (T)pool.Dequeue();
                var instanceTransform = instance.transform;
                instanceTransform.SetParent(parent, false);
                instanceTransform.SetPositionAndRotation(position, rotation);
                ResetBodyVelocity(instance);
                instance.gameObject.SetActive(true);
            }
            else
            {
                var instanceObject = resolver.Instantiate(prefab.gameObject, position, rotation, parent);
                instance = instanceObject.GetComponent<T>();
                prefabByInstance[instance] = prefab;
            }

            return instance;
        }

        public void Despawn(Component instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!prefabByInstance.TryGetValue(instance, out var prefab))
            {
                Object.Destroy(instance.gameObject);
                return;
            }

            if (!poolByPrefab.TryGetValue(prefab, out var pool))
            {
                pool = new Queue<Component>();
                poolByPrefab[prefab] = pool;
            }

            instance.gameObject.SetActive(false);
            ResetBodyVelocity(instance);
            instance.transform.SetParent(null, false);
            pool.Enqueue(instance);
        }

        private static void ResetBodyVelocity(Component instance)
        {
            if (instance.TryGetComponent(out Rigidbody body))
            {
                if (body.isKinematic)
                {
                    return;
                }

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
    }
}
