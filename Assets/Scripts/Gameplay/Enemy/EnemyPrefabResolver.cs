namespace DinoGrow.Gameplay.Enemy
{
    public static class EnemyPrefabResolver
    {
        public static DinoEnemy FindByName(DinoEnemy[] prefabs, string prefabName)
        {
            if (string.IsNullOrWhiteSpace(prefabName) || prefabs == null)
            {
                return null;
            }

            foreach (var prefab in prefabs)
            {
                if (prefab != null && prefab.name == prefabName)
                {
                    return prefab;
                }
            }

            return null;
        }
    }
}
