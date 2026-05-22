using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;
using DinoGrow.Infrastructure.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace DinoGrow.UI
{
    [ExecuteAlways]
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private Text levelText;
        [SerializeField] private Text expText;
        [SerializeField] private Text statusText;
        [SerializeField] private StatusTextView statusTextView;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button restartButton;
        [Header("Game Over Panel")]
        [SerializeField] private bool showGameOverPreviewInEditMode = true;
        [SerializeField] private string gameOverMessage = "GAME OVER";
        [SerializeField] private string clearMessage = "LEVEL 20 CLEAR";

        private PlayerProgress progress;
        private GameEventBus eventBus;
        private bool subscribedToEvents;

        [Inject]
        public void Construct(PlayerProgress progress, GameEventBus eventBus)
        {
            this.progress = progress;
            this.eventBus = eventBus;
            SubscribeToEvents();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                ApplyEditModeStatusPreview();
                return;
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartCurrentScene);
            }

            SubscribeToEvents();
        }

        private void Start()
        {
            EnsureStatusTextView();

            if (!Application.isPlaying)
            {
                ApplyEditModeStatusPreview();
                return;
            }

            Refresh();
            SetStatus("");
            SetGameOverPanelVisible(false);
        }

        private void OnValidate()
        {
            EnsureStatusTextView();
            ApplyEditModeStatusPreview();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartCurrentScene);
            }

            UnsubscribeFromEvents();
        }

        private void OnPlayerGrowthChanged(GrowthResult result)
        {
            Refresh();
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                SetStatus(gameOverMessage);
                SetGameOverPanelVisible(true);
            }
            else if (state == GameState.Clear)
            {
                SetStatus(clearMessage);
                SetGameOverPanelVisible(false);
            }
            else
            {
                SetStatus("");
                SetGameOverPanelVisible(false);
            }
        }

        private void Refresh()
        {
            if (progress == null)
            {
                return;
            }

            if (levelText != null)
            {
                levelText.text = $"Lv. {progress.Level}";
            }

            if (expText != null)
            {
                expText.text = progress.IsMaxLevel ? "EXP MAX" : $"EXP {progress.CurrentExp} / {progress.ExpToLevelUp}";
            }
        }

        private void SetStatus(string value)
        {
            if (statusTextView != null)
            {
                statusTextView.SetText(value);
                return;
            }

            if (statusText != null)
            {
                statusText.text = value;
                statusText.enabled = !string.IsNullOrEmpty(value);
            }
        }

        private void EnsureStatusTextView()
        {
            if (statusTextView == null && statusText != null)
            {
                statusTextView = statusText.GetComponent<StatusTextView>();
            }
        }

        private void ApplyEditModeStatusPreview()
        {
            if (Application.isPlaying)
            {
                return;
            }

            var previewVisible = showGameOverPreviewInEditMode;
            SetStatus(previewVisible ? gameOverMessage : string.Empty);
            SetGameOverPanelActiveOnly(previewVisible);
        }

        private void SubscribeToEvents()
        {
            if (eventBus == null || subscribedToEvents)
            {
                return;
            }

            eventBus.PlayerGrowthChanged += OnPlayerGrowthChanged;
            eventBus.GameStateChanged += OnGameStateChanged;
            subscribedToEvents = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (eventBus == null || !subscribedToEvents)
            {
                return;
            }

            eventBus.PlayerGrowthChanged -= OnPlayerGrowthChanged;
            eventBus.GameStateChanged -= OnGameStateChanged;
            subscribedToEvents = false;
        }

        private void SetGameOverPanelVisible(bool visible)
        {
            SetGameOverPanelActiveOnly(visible);

            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = visible;
        }

        private void SetGameOverPanelActiveOnly(bool visible)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(visible);
                return;
            }

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(visible);
            }
        }

        private static void RestartCurrentScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
                return;
            }

            SceneManager.LoadScene(activeScene.path);
        }
    }
}
