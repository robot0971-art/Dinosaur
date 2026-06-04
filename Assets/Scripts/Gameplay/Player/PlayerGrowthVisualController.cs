using DinoGrow.Core.Growth;
using DinoGrow.Infrastructure.Data;
using UnityEngine;

namespace DinoGrow.Gameplay.Player
{
    public sealed class PlayerGrowthVisualController
    {
        private readonly Transform ownerTransform;
        private readonly Transform visualRoot;
        private readonly PlayerGrowthDataRepository playerGrowthDataRepository;
        private readonly PlayerMovementMotor movementMotor;
        private readonly bool applyGrowthScale;
        private readonly float visualGroundOffset;
        private Vector3 baseVisualScale;
        private readonly Vector3 baseVisualLocalPosition;

        public PlayerGrowthVisualController(
            Transform ownerTransform,
            Transform visualRoot,
            PlayerGrowthDataRepository playerGrowthDataRepository,
            PlayerMovementMotor movementMotor,
            bool applyGrowthScale,
            float visualGroundOffset,
            Vector3 baseVisualScale,
            Vector3 baseVisualLocalPosition)
        {
            this.ownerTransform = ownerTransform;
            this.visualRoot = visualRoot;
            this.playerGrowthDataRepository = playerGrowthDataRepository;
            this.movementMotor = movementMotor;
            this.applyGrowthScale = applyGrowthScale;
            this.visualGroundOffset = visualGroundOffset;
            this.baseVisualScale = baseVisualScale;
            this.baseVisualLocalPosition = baseVisualLocalPosition;
        }

        public void SetBaseVisualScale(Vector3 scale)
        {
            baseVisualScale = scale;
        }

        public void ApplyGrowthVisuals(PlayerProgress progress)
        {
            if (!applyGrowthScale || progress == null)
            {
                return;
            }

            ApplyVisualGroundOffset();
            var growthScale = GetGrowthScale(progress.Level);
            visualRoot.localScale = baseVisualScale * growthScale;
            movementMotor?.CacheVisualBottomOffset();
            movementMotor?.CacheObstacleShape();
        }

        public void ApplyVisualGroundOffset()
        {
            if (visualRoot == null || visualRoot == ownerTransform)
            {
                return;
            }

            var localPosition = baseVisualLocalPosition;
            localPosition.y += visualGroundOffset;
            visualRoot.localPosition = localPosition;
        }

        private float GetGrowthScale(int level)
        {
            if (playerGrowthDataRepository != null
                && playerGrowthDataRepository.TryGetByLevel(level, out var growthData)
                && growthData.scaleMultiplier > 0f)
            {
                return growthData.scaleMultiplier;
            }

            return Mathf.Lerp(1f, 4.25f, Mathf.InverseLerp(1f, 20f, level));
        }
    }
}
