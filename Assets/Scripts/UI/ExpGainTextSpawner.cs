using UnityEngine;

namespace DinoGrow.UI
{
    /// <summary>
    /// EXP 획득 텍스트를 월드 위치에 생성하는 스크립트입니다.
    /// 기능 17에서는 텍스트 생성만 담당합니다.
    /// </summary>
    [ExecuteAlways]
    public sealed class ExpGainTextSpawner : MonoBehaviour
    {
        [Header("카메라 연결")]
        [Tooltip("EXP 텍스트가 바라볼 카메라입니다. Main Camera를 직접 드래그해서 넣어주세요")]
        [SerializeField] private Transform cameraTransform;

        [Header("생성 설정")]
        [Tooltip("텍스트를 기준 위치보다 얼마나 위에 만들지 정합니다")]
        [SerializeField] private Vector3 spawnOffset = new(0f, 2f, 0f);

        [Tooltip("EXP 텍스트 색상입니다")]
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

        [Header("초보자 테스트")]
        [Tooltip("Play를 눌렀을 때 자동으로 테스트 EXP 텍스트를 만들지 정합니다")]
        [SerializeField] private bool spawnTestTextOnStart = true;

        [Tooltip("테스트 텍스트를 만들 위치입니다. 적 공룡 Transform을 넣으면 적 공룡 위에 생성됩니다")]
        [SerializeField] private Transform testTarget;

        [Tooltip("테스트할 EXP 양입니다")]
        [SerializeField] private int testExpAmount = 40;

        [Header("에디터 표시")]
        [Tooltip("Play를 누르지 않아도 Scene 창에 EXP 텍스트를 보여줍니다")]
        [SerializeField] private bool showTextInEditMode = true;

        [Tooltip("Play를 누르지 않아도 보이는 EXP 텍스트 오브젝트입니다. 위치를 직접 움직여도 됩니다")]
        [SerializeField] private ExpGainTextView editModeExpText;

        private const string EditModeExpObjectName = "ExpGainText";

        private void OnEnable()
        {
            RefreshTextInEditMode();
        }

        private void OnValidate()
        {
            RefreshTextInEditMode();
        }

        private void Start()
        {
            // Play 시작 때 테스트용 ExpGainText를 자동 생성하지 않습니다.
            // 실제 게임에서는 공룡을 먹었을 때 SpawnExpText()를 호출해서 생성합니다.
        }

        /// <summary>
        /// 외부 스크립트가 EXP 획득 텍스트를 생성하고 싶을 때 호출합니다.
        /// worldPosition은 먹힌 공룡 위치 같은 월드 좌표입니다.
        /// expAmount가 40이면 "EXP +40" 텍스트가 생성됩니다.
        /// </summary>
        public ExpGainTextView SpawnExpText(Vector3 worldPosition, int expAmount)
        {
            var textObject = new GameObject("ExpGainText");
            textObject.transform.position = worldPosition + spawnOffset;

            var textView = textObject.AddComponent<ExpGainTextView>();
            textView.Initialize(
                expAmount,
                cameraTransform,
                textColor,
                characterSize,
                fontSize,
                moveSpeed,
                fadeDuration,
                autoDestroy);

            return textView;
        }

        /// <summary>
        /// Inspector 버튼은 아니지만, 다른 테스트 스크립트나 이벤트에서 쉽게 호출할 수 있는 함수입니다.
        /// </summary>
        public void SpawnTestText()
        {
            var spawnPosition = testTarget != null ? testTarget.position : transform.position;
            SpawnExpText(spawnPosition, testExpAmount);
        }

        private void RefreshTextInEditMode()
        {
            if (Application.isPlaying || !showTextInEditMode)
            {
                return;
            }

            if (editModeExpText == null)
            {
                editModeExpText = CreateEditModeText(EditModeExpObjectName);
            }

            if (editModeExpText != null)
            {
                // 이미 만들어진 ExpGainText 오브젝트 위치도 건드리지 않습니다.
                // 사용자가 Play 전에 직접 원하는 곳으로 배치할 수 있게 하기 위해서입니다.
                editModeExpText.Initialize(
                    testExpAmount,
                    cameraTransform,
                    textColor,
                    characterSize,
                    fontSize,
                    moveSpeed,
                    fadeDuration,
                    false);
            }
        }

        private ExpGainTextView CreateEditModeText(string objectName)
        {
            var textObject = new GameObject(objectName);
            var spawnPosition = testTarget != null ? testTarget.position : transform.position;
            textObject.transform.position = spawnPosition + spawnOffset;
            return textObject.AddComponent<ExpGainTextView>();
        }
    }
}
