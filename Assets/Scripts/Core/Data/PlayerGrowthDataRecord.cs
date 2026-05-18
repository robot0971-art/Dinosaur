using System;

namespace DinoGrow.Core.Data
{
    [Serializable]
    public sealed class PlayerGrowthDataRecord
    {
        public int level;
        public int requiredExp;
        public float scaleMultiplier;
        public float cameraDistance;
        public float cameraHeight;
    }
}
