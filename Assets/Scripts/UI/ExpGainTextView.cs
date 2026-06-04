using UnityEngine;

namespace DinoGrow.UI
{
    public sealed class ExpGainTextView : MonoBehaviour
    {
        [Header("Text")]
        [Tooltip("TextMesh used to show EXP gain text. One will be created automatically if empty.")]
        [SerializeField] private TextMesh expText;

        [Tooltip("Camera that this text faces. Assign explicitly to avoid scene searches.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Style")]
        [SerializeField] private Color textColor = new(1f, 0.87f, 0f, 1f);
        [SerializeField] private float characterSize = 4f;
        [SerializeField] private int fontSize = 64;

        [Header("Motion")]
        [SerializeField] private float moveSpeed = 1f;

        [Header("Fade")]
        [SerializeField] private float fadeDuration = 0.8f;

        [Header("Lifetime")]
        [SerializeField] private bool autoDestroy = true;

        private float elapsedTime;
        private float baseAlpha = 1f;
        private bool isDestroying;

        private void Awake()
        {
            EnsureTextMesh();
            ApplyTextStyle();
        }

        private void LateUpdate()
        {
            MoveUpDuringPlay();
            FadeOutDuringPlay();
            FaceCamera();
        }

        public void Initialize(
            int expAmount,
            Transform targetCamera,
            Color color,
            float size,
            int textFontSize,
            float speed,
            float duration,
            bool destroyAfterFade)
        {
            cameraTransform = targetCamera;
            textColor = color;
            characterSize = size;
            fontSize = textFontSize;
            moveSpeed = Mathf.Max(0f, speed);
            fadeDuration = Mathf.Max(0.01f, duration);
            autoDestroy = destroyAfterFade;
            elapsedTime = 0f;
            baseAlpha = textColor.a;
            isDestroying = false;

            SetExpAmount(expAmount);
            ApplyTextStyle();
            FaceCamera();
        }

        public void SetMoveSpeed(float speed)
        {
            moveSpeed = Mathf.Max(0f, speed);
        }

        public void SetFadeDuration(float duration)
        {
            fadeDuration = Mathf.Max(0.01f, duration);
        }

        public void SetAutoDestroy(bool value)
        {
            autoDestroy = value;
        }

        public void SetCamera(Transform targetCamera)
        {
            cameraTransform = targetCamera;
            FaceCamera();
        }

        public void SetExpAmount(int expAmount)
        {
            EnsureTextMesh();

            if (expText == null)
            {
                return;
            }

            expText.text = string.Concat("EXP +", Mathf.Max(0, expAmount).ToString());
        }

        private void EnsureTextMesh()
        {
            if (expText != null)
            {
                return;
            }

            expText = GetComponent<TextMesh>();
            if (expText == null)
            {
                expText = gameObject.AddComponent<TextMesh>();
            }
        }

        private void ApplyTextStyle()
        {
            if (expText == null)
            {
                return;
            }

            expText.anchor = TextAnchor.MiddleCenter;
            expText.alignment = TextAlignment.Center;
            expText.characterSize = characterSize;
            expText.fontSize = fontSize;
            expText.color = textColor;
            ConfigureFontMaterial(expText);
        }

        private void FaceCamera()
        {
            if (expText == null || cameraTransform == null)
            {
                return;
            }

            var lookDirection = transform.position - cameraTransform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
        }

        private void MoveUpDuringPlay()
        {
            if (!Application.isPlaying || moveSpeed <= 0f)
            {
                return;
            }

            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        }

        private void FadeOutDuringPlay()
        {
            if (!Application.isPlaying || expText == null)
            {
                return;
            }

            elapsedTime += Time.deltaTime;

            var progress = Mathf.Clamp01(elapsedTime / fadeDuration);
            var alpha = Mathf.Lerp(baseAlpha, 0f, progress);
            var color = expText.color;
            color.a = alpha;
            expText.color = color;

            TryDestroyAfterFade(progress);
        }

        private void TryDestroyAfterFade(float fadeProgress)
        {
            if (!Application.isPlaying || !autoDestroy || isDestroying)
            {
                return;
            }

            if (fadeProgress < 1f)
            {
                return;
            }

            isDestroying = true;
            Destroy(gameObject);
        }

        private static void ConfigureFontMaterial(TextMesh targetText)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            if (font == null)
            {
                return;
            }

            targetText.font = font;

            if (targetText.TryGetComponent<MeshRenderer>(out var textRenderer))
            {
                textRenderer.sharedMaterial = font.material;
            }
        }
    }
}
