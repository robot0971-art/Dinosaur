using System;

namespace DinoGrow.Core.Data
{
    [Serializable]
    public sealed class SpawnDataRecord
    {
        public int stageId;
        public string dinoId;
        public int minLevel;
        public int maxLevel;
        public int count;
        public int weight;
        public float minWanderSpeed;
        public float maxWanderSpeed;
    }
}
