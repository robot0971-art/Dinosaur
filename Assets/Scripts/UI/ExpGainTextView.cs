using UnityEngine;

namespace DinoGrow.UI
{
    /// <summary>
    /// 월드에 생성된 EXP 획득 텍스트 1개를 표시하는 스크립트입니다.
    /// 기능 20에서는 Play 중 투명화가 끝난 뒤 자동 삭제하는 기능까지 담당합니다.
    /// </summary>
    public sealed class ExpGainTextView : MonoBehaviour
    {
        [Header("EXP 텍스트 연결")]
        [Tooltip("EXP +40 같은 글자를 표시할 TextMesh입니다. 비워두면 자동으로 만들어집니다")]
        [SerializeField] private TextMesh expText;

        [Tooltip("텍스트가 바라볼 카메라입니다. GameObject.Find나 Camera.main을 쓰지 않기 위해 직접 연결합니다")]
        [SerializeField] private Transform cameraTransform;

        [Header("EXP 텍스트 모양")]
        [Tooltip("텍스트 색상입니다. 기능 목록 기준 노란색을 사용합니다")]
        [SerializeField] private Color textColor = new(1f, 0.87f, 0f, 1f);

        [Tooltip("월드 공간에서 보이는 글자 크기입니다")]
        [SerializeField] private float characterSize = 4f;

        [Tooltip("폰트 해상도입니다. 글자가 흐리면 값을 키워보세요")]
        [SerializeField] private int fontSize = 64;

        [Header("위로 이동 설정")]
        [Tooltip("Play 중 EXP 텍스트가 1초에 얼마나 위로 올라갈지 정합니다")]
        [SerializeField] private float moveSpeed = 1f;

        [Header("투명화 설정")]
        [Tooltip("Play 중 EXP 텍스트가 몇 초 동안 서서히 투명해질지 정합니다")]
        [SerializeField] private float fadeDuration = 0.8f;

        [Header("자동 삭제 설정")]
        [Tooltip("Play 중 투명화가 끝난 EXP 텍스트를 자동으로 삭제할지 정합니다")]
        [SerializeField] private bool autoDestroy = true;

        private float elapsedTime;
        private float baseAlpha = 1f;
        private bool isDestroying;

        private void Awake()
        {
            // 오브젝트가 만들어질 때 TextMesh가 없으면 자동으로 준비합니다.
            EnsureTextMesh();
            ApplyTextStyle();
        }

        private void LateUpdate()
        {
            // Play 중일 때만 위로 움직입니다.
            // 에디터 상태에서는 직접 잡고 위치를 조절할 수 있어야 하므로 움직이지 않습니다.
            MoveUpDuringPlay();

            // Play 중일 때만 점점 투명하게 만듭니다.
            // 에디터 상태에서는 위치 조절용 텍스트가 사라지면 안 되므로 투명화하지 않습니다.
            FadeOutDuringPlay();

            // 카메라가 움직인 뒤 마지막에 텍스트가 카메라를 바라보게 합니다.
            FaceCamera();
        }

        /// <summary>
        /// 스포너가 새 EXP 텍스트를 만들 때 호출합니다.
        /// 예: Initialize(40, 카메라, 노란색, 4, 64, 1, 0.8, true)
        /// </summary>
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

        /// <summary>
        /// 다른 스크립트가 이동 속도를 바꾸고 싶을 때 사용합니다.
        /// speed가 1이면 1초에 1 유닛 위로 이동합니다.
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>
        /// 다른 스크립트가 투명화 시간을 바꾸고 싶을 때 사용합니다.
        /// duration이 0.8이면 0.8초 동안 서서히 투명해집니다.
        /// </summary>
        public void SetFadeDuration(float duration)
        {
            fadeDuration = Mathf.Max(0.01f, duration);
        }

        /// <summary>
        /// 다른 스크립트가 자동 삭제 여부를 바꾸고 싶을 때 사용합니다.
        /// true이면 투명화가 끝난 뒤 Play 중에 오브젝트를 삭제합니다.
        /// </summary>
        public void SetAutoDestroy(bool value)
        {
            autoDestroy = value;
        }

        /// <summary>
        /// 카메라 Transform을 외부에서 연결할 때 사용합니다.
        /// 씬 검색을 하지 않기 위한 직접 연결 방식입니다.
        /// </summary>
        public void SetCamera(Transform targetCamera)
        {
            cameraTransform = targetCamera;
            FaceCamera();
        }

        /// <summary>
        /// EXP 숫자를 텍스트에 표시합니다.
        /// expAmount가 40이면 "EXP +40"으로 보입니다.
        /// </summary>
        public void SetExpAmount(int expAmount)
        {
            EnsureTextMesh();

            if (expText == null)
            {
                return;
            }

            expText.text = $"EXP +{Mathf.Max(0, expAmount)}";
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
