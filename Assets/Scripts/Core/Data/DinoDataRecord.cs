using System;

namespace DinoGrow.Core.Data
{
    [Serializable]
    public sealed class DinoDataRecord
    {
        public string id;
        public string displayName;
        public int level;
        public int exp;
        public float speed;
        public float size;
        public string aiType;
        public string colorType;
        public string prefab;
    }
}
