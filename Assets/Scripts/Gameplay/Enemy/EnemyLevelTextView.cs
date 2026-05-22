using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    /// <summary>
    /// 적 머리 위에 보이는 레벨 텍스트만 관리하는 스크립트입니다.
    /// 이동, 충돌, EXP, 게임오버 같은 게임 규칙은 처리하지 않습니다.
    /// </summary>
    public sealed class EnemyLevelTextView : MonoBehaviour
    {
        [Header("레벨 텍스트 연결")]
        [Tooltip("적 머리 위에 표시할 TextMesh입니다. 비워두면 자동으로 만들어집니다")]
        [SerializeField] private TextMesh levelText;

        [Tooltip("텍스트가 따라다닐 적 공룡의 기준 Transform입니다. 보통 적 공룡 루트입니다")]
        [SerializeField] private Transform targetRoot;

        [Tooltip("텍스트가 바라볼 카메라 Transform입니다. GameObject.Find나 Camera.main을 쓰지 않기 위해 직접 연결합니다")]
        [SerializeField] private Transform cameraTransform;

        [Header("레벨 텍스트 모양")]
        [Tooltip("공룡 몸 위에서 텍스트를 얼마나 더 위로 올릴지 정합니다")]
        [SerializeField] private float heightPadding = 0.5f;

        [Tooltip("월드 공간에서 보이는 글자 크기입니다")]
        [SerializeField] private float characterSize = 0.12f;

        [Tooltip("폰트 해상도입니다. 글자가 흐리면 조금 키워보세요")]
        [SerializeField] private int fontSize = 48;

        [Tooltip("레벨 텍스트 색상입니다. 기능 13에서는 기본 흰색만 사용합니다")]
        [SerializeField] private Color textColor = Color.white;

        private int currentLevel = 1;

        private void Awake()
        {
            // 씬 시작 전에 TextMesh가 없으면 자동으로 준비합니다.
            EnsureTextMesh();
            Refresh();
        }

        private void LateUpdate()
        {
            // LateUpdate는 일반 Update 뒤에 실행됩니다.
            // 공룡이 이동한 뒤 마지막에 텍스트 위치를 맞추기 좋습니다.
            RefreshPosition();
            FaceCamera();
        }

        /// <summary>
        /// 다른 스크립트가 적 레벨을 바꿀 때 호출합니다.
        /// 예: SetLevel(3)을 호출하면 화면에 "Lv. 3"이 표시됩니다.
        /// </summary>
        public void SetLevel(int level)
        {
            currentLevel = Mathf.Clamp(level, 1, 20);
            Refresh();
        }

        /// <summary>
        /// 카메라 Transform을 외부에서 연결할 때 호출합니다.
        /// 씬 검색을 하지 않기 위해 직접 주입받는 방식입니다.
        /// </summary>
        public void SetCamera(Transform targetCamera)
        {
            cameraTransform = targetCamera;
            FaceCamera();
        }

        /// <summary>
        /// TextMesh, 기준 Transform, 높이, 크기, 색상을 한 번에 설정합니다.
        /// DinoEnemy가 기존 Inspector 값을 그대로 넘겨줄 때 사용합니다.
        /// </summary>
        public void Configure(
            TextMesh targetText,
            Transform root,
            float textHeightPadding,
            float textCharacterSize,
            int textFontSize,
            Color color)
        {
            levelText = targetText;
            targetRoot = root;
            heightPadding = textHeightPadding;
            characterSize = textCharacterSize;
            fontSize = textFontSize;
            textColor = color;
            Refresh();
        }

        /// <summary>
        /// 현재 설정값으로 텍스트 내용을 다시 표시합니다.
        /// Inspector 값을 바꾼 뒤 다시 적용할 때 사용합니다.
        /// </summary>
        public void Refresh()
        {
            EnsureTextMesh();

            if (levelText == null)
            {
                return;
            }

            levelText.text = $"Lv. {currentLevel}";
            levelText.anchor = TextAnchor.MiddleCenter;
            levelText.alignment = TextAlignment.Center;
            levelText.characterSize = characterSize;
            levelText.fontSize = fontSize;
            levelText.color = textColor;
            ConfigureFontMaterial(levelText);
            RefreshPosition();
            FaceCamera();
        }

        /// <summary>
        /// 기능 14~16에서 색상 구분을 만들 때도 재사용할 수 있도록 색상만 바꾸는 함수입니다.
        /// 기능 13에서는 흰색으로만 사용하면 됩니다.
        /// </summary>
        public void SetColor(Color color)
        {
            textColor = color;

            if (levelText != null)
            {
                levelText.color = textColor;
            }
        }

        /// <summary>
        /// 하트나 죽음 연출처럼 텍스트를 잠깐 숨길 때 사용합니다.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (levelText != null)
            {
                levelText.gameObject.SetActive(visible);
            }
        }

        private void EnsureTextMesh()
        {
            if (targetRoot == null)
            {
                targetRoot = transform;
            }

            if (levelText != null)
            {
                return;
            }

            var labelObject = new GameObject("EnemyLevelText");
            labelObject.transform.SetParent(targetRoot, false);
            levelText = labelObject.AddComponent<TextMesh>();
        }

        private void RefreshPosition()
        {
            if (levelText == null || targetRoot == null)
            {
                return;
            }

            levelText.transform.position = GetTextPosition();
        }

        private void FaceCamera()
        {
            if (levelText == null || cameraTransform == null)
            {
                return;
            }

            var lookDirection = levelText.transform.position - cameraTransform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                levelText.transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
        }

        private Vector3 GetTextPosition()
        {
            var renderers = targetRoot.GetComponentsInChildren<Renderer>();
            var hasBounds = false;
            var bounds = new Bounds(targetRoot.position, Vector3.zero);

            foreach (var targetRenderer in renderers)
            {
                // 레벨 텍스트 자신의 Renderer는 몸 크기 계산에서 제외합니다.
                if (targetRenderer.GetComponent<TextMesh>() != null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = targetRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(targetRenderer.bounds);
                }
            }

            if (!hasBounds)
            {
                return targetRoot.position + Vector3.up * heightPadding;
            }

            return new Vector3(bounds.center.x, bounds.max.y + heightPadding, bounds.center.z);
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
