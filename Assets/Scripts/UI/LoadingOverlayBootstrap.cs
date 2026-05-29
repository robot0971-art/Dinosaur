using UnityEngine;
using UnityEngine.UI;

namespace DinoGrow.UI
{
    [DefaultExecutionOrder(-20000)]
    public sealed class LoadingOverlayBootstrap : MonoBehaviour
    {
        [SerializeField] private Slider loadingSlider;
        [SerializeField] private bool logDiagnostics = true;

        private void Awake()
        {
            Show();
            LogState("Awake");
        }

        private void OnEnable()
        {
            Show();
            LogState("OnEnable");
        }

        private void Start()
        {
            LogState("Start");
        }

        private void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue;

            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 1f;

            if (loadingSlider != null)
            {
                loadingSlider.value = 0f;
            }
        }

        private void LogState(string source)
        {
            if (!logDiagnostics)
            {
                return;
            }

            var rect = transform as RectTransform;
            var canvas = GetComponent<Canvas>();
            var canvasGroup = GetComponent<CanvasGroup>();
            var image = GetComponent<Image>();

            Debug.Log(
                $"[LoadingOverlay] {source} " +
                $"activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}, " +
                $"parent={(transform.parent != null ? transform.parent.name : "null")}, sibling={transform.GetSiblingIndex()}, " +
                $"rectSize={(rect != null ? rect.rect.size.ToString() : "null")}, scale={transform.lossyScale}, " +
                $"canvas={(canvas != null ? $"override={canvas.overrideSorting}, order={canvas.sortingOrder}, enabled={canvas.enabled}" : "null")}, " +
                $"groupAlpha={(canvasGroup != null ? canvasGroup.alpha.ToString("0.###") : "null")}, " +
                $"image={(image != null ? $"enabled={image.enabled}, color={image.color}, sprite={(image.sprite != null ? image.sprite.name : "null")}" : "null")}, " +
                $"slider={(loadingSlider != null ? $"value={loadingSlider.value:0.###}, active={loadingSlider.gameObject.activeInHierarchy}" : "null")}",
                this);
        }
    }
}
