using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class DinoEnemy : MonoBehaviour
    {
        [SerializeField] private int level = 1;
        [SerializeField] private bool usePrototypeLevelMaterial;
        [SerializeField] private Color levelOneColor = new(0.25f, 0.95f, 0.35f);
        [SerializeField] private Color levelTwoColor = new(0.2f, 0.65f, 1f);
        [SerializeField] private Color levelThreeColor = new(1f, 0.35f, 0.2f);

        private static Material[] prototypeMaterials;

        public int Level => level;

        private void Awake()
        {
            ApplyPrototypeMaterial();
        }

        public void SetLevel(int value)
        {
            level = Mathf.Clamp(value, 1, 20);
            ApplyPrototypeMaterial();
        }

        public void Eaten()
        {
            Destroy(gameObject);
        }

        private void ApplyPrototypeMaterial()
        {
            if (!usePrototypeLevelMaterial)
            {
                return;
            }

            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer.GetComponent<TextMesh>() != null)
                {
                    continue;
                }

                targetRenderer.sharedMaterial = GetMaterialForLevel(level);
            }
        }

        private Material GetMaterialForLevel(int targetLevel)
        {
            prototypeMaterials ??= new Material[3];
            var index = Mathf.Clamp(targetLevel, 1, 3) - 1;
            if (prototypeMaterials[index] != null)
            {
                return prototypeMaterials[index];
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                name = $"Prototype Enemy Lv{index + 1}"
            };

            SetMaterialColor(material, GetColorForLevel(index + 1));
            prototypeMaterials[index] = material;
            return material;
        }

        private Color GetColorForLevel(int targetLevel)
        {
            return targetLevel switch
            {
                1 => levelOneColor,
                2 => levelTwoColor,
                _ => levelThreeColor
            };
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }
    }
}
