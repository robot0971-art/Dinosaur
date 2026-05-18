using System;

namespace DinoGrow.Core.Data
{
    [Serializable]
    public sealed class StageDataRecord
    {
        public int stageId;
        public string displayName;
        public float spawnCenterX;
        public float spawnCenterZ;
        public float spawnSizeX;
        public float spawnSizeZ;
        public float spawnY;
        public float minDistanceFromPlayer;
        public float timeLimit;
    }
}
