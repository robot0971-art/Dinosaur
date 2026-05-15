namespace DinoGrow.Core.Stage
{
    public sealed class StageRule
    {
        private const int ClearLevel = 20;

        public bool IsClearLevel(int playerLevel)
        {
            return playerLevel >= ClearLevel;
        }
    }
}
