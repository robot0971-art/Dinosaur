using DinoGrow.Gameplay.Enemy;
using UnityEngine;

namespace DinoGrow.Gameplay.Player
{
    public sealed class PlayerEnemyContactScanner
    {
        private readonly Collider[] contactColliders;

        public PlayerEnemyContactScanner(int maxContacts)
        {
            contactColliders = new Collider[Mathf.Max(1, maxContacts)];
        }

        public bool TryFindContact(
            Transform playerTransform,
            float contactRadius,
            LayerMask contactLayers,
            out DinoEnemy enemy)
        {
            enemy = null;
            if (playerTransform == null)
            {
                return false;
            }

            var radius = Mathf.Max(0.1f, contactRadius * Mathf.Max(1f, playerTransform.lossyScale.x));
            if (TryFindPhysicsContact(playerTransform, radius, contactLayers, out enemy))
            {
                return true;
            }

            return TryFindRegisteredContact(playerTransform, radius, out enemy);
        }

        private bool TryFindPhysicsContact(
            Transform playerTransform,
            float radius,
            LayerMask contactLayers,
            out DinoEnemy enemy)
        {
            enemy = null;
            var overlapCount = Physics.OverlapSphereNonAlloc(
                playerTransform.position,
                radius,
                contactColliders,
                contactLayers,
                QueryTriggerInteraction.Collide);

            for (var i = 0; i < overlapCount; i++)
            {
                var overlap = contactColliders[i];
                var candidate = overlap.GetComponentInParent<DinoEnemy>();
                if (!IsValidContact(playerTransform, candidate, radius))
                {
                    continue;
                }

                enemy = candidate;
                return true;
            }

            return false;
        }

        private static bool TryFindRegisteredContact(Transform playerTransform, float playerRadius, out DinoEnemy enemy)
        {
            enemy = null;
            var enemies = DinoEnemy.Active;
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                var candidate = enemies[i];
                if (candidate == null || candidate.IsDying || !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var offset = candidate.transform.position - playerTransform.position;
                offset.y = 0f;
                var contactDistance = playerRadius + candidate.GetContactRadius();
                if (offset.sqrMagnitude > contactDistance * contactDistance)
                {
                    continue;
                }

                enemy = candidate;
                return true;
            }

            return false;
        }

        private static bool IsValidContact(Transform playerTransform, DinoEnemy enemy, float radius)
        {
            if (enemy == null || enemy.IsDying)
            {
                return false;
            }

            var offset = enemy.transform.position - playerTransform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }
    }
}
