using UnityEngine;

namespace DinoGrow.Camera
{
    public sealed class EnvironmentSettingsController : MonoBehaviour
    {
        [Header("Skybox")]
        [SerializeField] private Material skyboxMaterial;

        [Header("Fog")]
        [SerializeField] private bool fogEnabled = true;
        [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;
        [SerializeField] private Color fogColor = new(0.78f, 0.68f, 0.5f, 1f);
        [Min(0f)]
        [SerializeField] private float fogDensity = 0.012f;
        [Min(0f)]
        [SerializeField] private float fogStartDistance;
        [Min(0f)]
        [SerializeField] private float fogEndDistance = 300f;

        public void Apply()
        {
            if (skyboxMaterial != null)
            {
                RenderSettings.skybox = skyboxMaterial;
                DynamicGI.UpdateEnvironment();
            }
            else
            {
                Debug.LogWarning($"{nameof(EnvironmentSettingsController)} on '{name}' has no skybox material assigned.", this);
            }

            RenderSettings.fog = fogEnabled;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = Mathf.Max(fogStartDistance, fogEndDistance);
        }

#if UNITY_EDITOR
        [ContextMenu("Apply Environment Settings")]
        private void ApplyFromContextMenu()
        {
            Apply();
        }
#endif
    }
}
