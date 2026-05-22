namespace DinoGrow.Core.Combat
{
    public sealed class EatResolver
    {
        public EatResult Resolve(int playerLevel, int enemyLevel)
        {
            if (playerLevel >= enemyLevel)
            {
                return EatResult.Eat;
            }

            return EatResult.GameOver;
        }
    }
}
