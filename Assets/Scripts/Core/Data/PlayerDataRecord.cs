using System;

namespace DinoGrow.Core.Data
{
    [Serializable]
    public sealed class PlayerDataRecord
    {
        public string id;
        public string displayName;
        public int level;
        public int exp;
        public float speed;
        public float size;
        public string prefab;
        public int maxLives = 3;
    }
}
