using UnityEngine;
using UnityEngine.SceneManagement;

namespace DinoGrow.Gameplay.Stage
{
    internal readonly struct StageMapBoundaryResolver
    {
        private readonly string boundaryRootName;
        private readonly float boundaryInset;

        public StageMapBoundaryResolver(string boundaryRootName, float boundaryInset)
        {
            this.boundaryRootName = boundaryRootName;
            this.boundaryInset = boundaryInset;
        }

        public bool TryGetBoundaryArea(Scene mapScene, out Vector3 center, out Vector2 size)
        {
            var hasBounds = false;
            var bounds = new Bounds();
            foreach (var boundaryRoot in StageSceneObjectUtility.FindAllInScene(mapScene, boundaryRootName))
            {
                var colliders = boundaryRoot.GetComponentsInChildren<Collider>(true);
                foreach (var targetCollider in colliders)
                {
                    if (targetCollider == null || targetCollider.isTrigger || !targetCollider.enabled)
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        bounds = targetCollider.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(targetCollider.bounds);
                    }
                }
            }

            if (!hasBounds)
            {
                center = Vector3.zero;
                size = Vector2.zero;
                return false;
            }

            var inset = Mathf.Max(0f, boundaryInset);
            center = new Vector3(bounds.center.x, 0f, bounds.center.z);
            size = new Vector2(
                Mathf.Max(1f, bounds.size.x - inset * 2f),
                Mathf.Max(1f, bounds.size.z - inset * 2f));
            return true;
        }
    }
}
