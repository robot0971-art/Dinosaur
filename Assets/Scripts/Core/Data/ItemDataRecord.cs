using System;

namespace DinoGrow.Core.Data
{
    [Serializable]
    public sealed class ItemDataRecord
    {
        public string id;
        public string displayName;
        public string effectType;
        public int effectValue;
        public string prefab;
    }
}
