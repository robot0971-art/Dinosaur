using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    public static class EnemyScaleApplier
    {
        public static void ApplyNormalizedScale(Transform enemyTransform, float targetSize, float sizeUnit)
        {
            var renderers = enemyTransform.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                enemyTransform.localScale = Vector3.one * targetSize;
                return;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var currentSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (currentSize <= 0.001f)
            {
                enemyTransform.localScale = Vector3.one * targetSize;
                return;
            }

            var targetWorldSize = Mathf.Max(0.1f, targetSize * sizeUnit);
            var scaleMultiplier = targetWorldSize / currentSize;
            enemyTransform.localScale *= scaleMultiplier;
        }
    }
}
