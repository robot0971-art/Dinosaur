using UnityEngine;

namespace DinoGrow.UI
{
    [ExecuteAlways]
    public sealed class ExpGainTextSpawner : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Transform cameraTransform;

        [Header("Spawn Settings")]
        [SerializeField] private Vector3 spawnOffset = new(0f, 2f, 0f);
        [SerializeField] private Color textColor = new(1f, 0.87f, 0f, 1f);
        [SerializeField] private float characterSize = 4f;
        [SerializeField] private int fontSize = 64;

        [Header("Motion")]
        [SerializeField] private float moveSpeed = 1f;

        [Header("Fade")]
        [SerializeField] private float fadeDuration = 0.8f;

        [Header("Lifetime")]
        [SerializeField] private bool autoDestroy = true;

        [Header("Test")]
        [SerializeField] private Transform testTarget;
        [SerializeField] private int testExpAmount = 40;

        [Header("Editor Preview")]
        [SerializeField] private bool showTextInEditMode = true;
        [SerializeField] private ExpGainTextView editModeExpText;

        private const string ExpObjectName = "ExpGainText";

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
            if (Application.isPlaying && editModeExpText != null)
            {
                editModeExpText.gameObject.SetActive(false);
            }
        }

        public ExpGainTextView SpawnExpText(Vector3 worldPosition, int expAmount)
        {
            var textObject = new GameObject(ExpObjectName);
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
                editModeExpText = CreateEditModeText(ExpObjectName);
            }

            if (editModeExpText == null)
            {
                return;
            }

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

        private ExpGainTextView CreateEditModeText(string objectName)
        {
            var textObject = new GameObject(objectName);
            var spawnPosition = testTarget != null ? testTarget.position : transform.position;
            textObject.transform.position = spawnPosition + spawnOffset;
            return textObject.AddComponent<ExpGainTextView>();
        }
    }
}
