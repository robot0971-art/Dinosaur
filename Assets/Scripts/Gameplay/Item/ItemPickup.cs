using DinoGrow.Core.Data;
using DinoGrow.Infrastructure.Data;
using UnityEngine;
using VContainer;

namespace DinoGrow.Gameplay.Item
{
    public sealed class ItemPickup : MonoBehaviour
    {
        [SerializeField] private string itemId = "heart";
        [SerializeField] private float destroyDelay = 0.1f;

        private HeartsSystem heartsSystem;
        private ItemDataRepository itemDataRepository;
        private bool consumed;

        [Inject]
        public void Construct(HeartsSystem heartsSystem, ItemDataRepository itemDataRepository)
        {
            this.heartsSystem = heartsSystem;
            this.itemDataRepository = itemDataRepository;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (consumed || heartsSystem == null || !heartsSystem.IsAlive)
            {
                return;
            }

            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (itemDataRepository != null && itemDataRepository.TryGetById(itemId, out var itemData))
            {
                if (itemData.effectType == "Heart")
                {
                    var added = itemData.effectValue > 0 ? itemData.effectValue : 1;
                    for (var i = 0; i < added; i++)
                    {
                        heartsSystem.AddLife();
                    }
                }
            }
            else
            {
                heartsSystem.AddLife();
            }

            consumed = true;
            Destroy(gameObject, destroyDelay);
        }
    }
}
