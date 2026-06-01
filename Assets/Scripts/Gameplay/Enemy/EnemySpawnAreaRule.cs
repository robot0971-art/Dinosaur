using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class EnemySpawnAreaRule
    {
        private Vector3 center;
        private Vector2 size;
        private float edgePaddingRatio;

        public EnemySpawnAreaRule(Vector3 center, Vector2 size, float edgePaddingRatio)
        {
            Configure(center, size, edgePaddingRatio);
        }

        public void Configure(Vector3 center, Vector2 size, float edgePaddingRatio)
        {
            this.center = center;
            this.size = new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
            this.edgePaddingRatio = Mathf.Clamp(edgePaddingRatio, 0f, 0.45f);
        }

        public bool Contains(Vector3 position)
        {
            var halfSize = size * 0.5f;
            return position.x >= center.x - halfSize.x
                && position.x <= center.x + halfSize.x
                && position.z >= center.z - halfSize.y
                && position.z <= center.z + halfSize.y;
        }

        public bool IsNearEdge(Vector3 position)
        {
            var halfSize = size * 0.5f;
            var padding = new Vector2(halfSize.x * edgePaddingRatio, halfSize.y * edgePaddingRatio);
            return position.x <= center.x - halfSize.x + padding.x
                || position.x >= center.x + halfSize.x - padding.x
                || position.z <= center.z - halfSize.y + padding.y
                || position.z >= center.z + halfSize.y - padding.y;
        }

        public Vector3 Clamp(Vector3 position)
        {
            var halfSize = size * 0.5f;
            position.x = Mathf.Clamp(position.x, center.x - halfSize.x, center.x + halfSize.x);
            position.z = Mathf.Clamp(position.z, center.z - halfSize.y, center.z + halfSize.y);
            return position;
        }

        public Vector3 RandomPosition(
            float y,
            float centerWeightedChance,
            float centerWeightedScale,
            System.Func<float> random01,
            System.Func<float, float, float> randomRange)
        {
            var halfSize = size * 0.5f;
            if (random01() < Mathf.Clamp01(centerWeightedChance))
            {
                halfSize *= Mathf.Clamp(centerWeightedScale, 0.1f, 1f);
            }

            var edgePadding = size * (0.5f * edgePaddingRatio);
            halfSize = new Vector2(
                Mathf.Max(1f, halfSize.x - edgePadding.x),
                Mathf.Max(1f, halfSize.y - edgePadding.y));

            return new Vector3(
                randomRange(center.x - halfSize.x, center.x + halfSize.x),
                y,
                randomRange(center.z - halfSize.y, center.z + halfSize.y));
        }
    }
}
