namespace DinoGrow.Core.Stage
{
    public sealed class StageRule
    {
        public bool IsClearLevel(int currentLevel)
        {
            return currentLevel >= 20;
        }
    }
}
