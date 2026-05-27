using UnityEngine;

namespace DinoGrow.Gameplay
{
    internal static class RendererBoundsUtility
    {
        public static bool TryCalculateVisibleBounds(Transform root, out Bounds bounds)
        {
            bounds = root != null ? new Bounds(root.position, Vector3.zero) : default;
            if (root == null)
            {
                return false;
            }

            var renderers = root.GetComponentsInChildren<Renderer>();
            var hasBounds = false;
            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer == null || targetRenderer.GetComponent<TextMesh>() != null)
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
