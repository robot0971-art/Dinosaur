using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class DinoEnemy : MonoBehaviour
    {
        [SerializeField] private int level = 1;

        public int Level => level;

        public void SetLevel(int value)
        {
            level = Mathf.Clamp(value, 1, 20);
        }

        public void Eaten()
        {
            Destroy(gameObject);
        }
    }
}
