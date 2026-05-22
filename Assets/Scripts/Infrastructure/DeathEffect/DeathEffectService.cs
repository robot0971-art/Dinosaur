using UnityEngine;

namespace DinoGrow.Infrastructure.Events
{
    public sealed class DeathEffectService
    {
        public void SpawnBlood(Vector3 position)
        {
            Debug.Log($"[DeathEffectService] Blood spawned at {position}");
        }
    }
}
