using UnityEngine;

namespace DinoGrow.Camera
{
    [ExecuteAlways]
    public sealed class EnvironmentSettingsController : MonoBehaviour
    {
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

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        public void Apply()
        {
            RenderSettings.fog = fogEnabled;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = Mathf.Max(fogStartDistance, fogEndDistance);
        }
    }
}
