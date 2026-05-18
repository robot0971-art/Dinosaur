namespace DinoGrow.Core.Combat
{
    public sealed class EatResolver
    {
        public EatResult Resolve(int playerLevel, int enemyLevel)
        {
            return playerLevel >= enemyLevel
                ? EatResult.Eat
                : EatResult.GameOver;
        }
    }
}
