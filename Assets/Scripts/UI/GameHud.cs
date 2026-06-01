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
        [SerializeField] private global::GameHudLevelExpPanel levelExpPanel;
        [SerializeField] private Text statusText;
        [SerializeField] private StatusTextView statusTextView;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button restartButton;
        [Header("Game Over Panel")]
        [SerializeField] private bool showGameOverPreviewInEditMode = true;
        [SerializeField] private string gameOverMessage = "GAME OVER";
        [SerializeField] private GameObject gameOverImage;
        [SerializeField] private string gameOverImageChildName = "Game over Image";
        [SerializeField] private AudioClip gameOverSoundClip;
        [SerializeField] private AudioSource gameOverSoundSource;
        [SerializeField, Range(0f, 1f)] private float gameOverSoundVolume = 1f;
        [SerializeField] private string clearMessage = "LEVEL 20 CLEAR";

        private PlayerProgress progress;
        private GameEventBus eventBus;
        private GameHudProgressPresenter progressPresenter;
        private GameHudStatusPresenter statusPresenter;
        private GameHudGameOverPresenter gameOverPresenter;
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
            EnsurePresenters();

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
            EnsurePresenters();
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
                EnsurePresenters();
                SetStatus(gameOverImage != null ? string.Empty : gameOverMessage);
                SetGameOverPanelVisible(true);
                gameOverPresenter.PlaySound();
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
            EnsurePresenters();
            progressPresenter.Refresh(progress);
        }

        private void SetStatus(string value)
        {
            EnsurePresenters();
            statusPresenter.SetText(value);
        }

        private void EnsurePresenters()
        {
            progressPresenter = new GameHudProgressPresenter(levelText, expText, levelExpPanel);
            if (statusTextView == null && statusText != null)
            {
                statusTextView = statusText.GetComponent<StatusTextView>();
            }

            statusPresenter = new GameHudStatusPresenter(statusText, statusTextView);
            gameOverPresenter = new GameHudGameOverPresenter(
                gameOverPanel,
                restartButton,
                gameOverImage,
                gameOverImageChildName,
                gameOverSoundClip,
                gameOverSoundSource,
                gameOverSoundVolume,
                gameObject);
            gameOverImage = gameOverPresenter.GameOverImage;
        }

        private void ApplyEditModeStatusPreview()
        {
            if (Application.isPlaying)
            {
                return;
            }

            var previewVisible = showGameOverPreviewInEditMode;
            EnsurePresenters();
            SetStatus(previewVisible && gameOverImage == null ? gameOverMessage : string.Empty);
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
            EnsurePresenters();
            gameOverPresenter.Show(visible, true);
        }

        private void SetGameOverPanelActiveOnly(bool visible)
        {
            EnsurePresenters();
            gameOverPresenter.SetActiveOnly(visible);
            gameOverImage = gameOverPresenter.GameOverImage;
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
