using DinoGrow.Gameplay;
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
                EnsureGameOverImage();
                SetStatus(gameOverImage != null ? string.Empty : gameOverMessage);
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

            if (levelExpPanel != null)
            {
                levelExpPanel.SetProgress(progress.Level, progress.CurrentExp, progress.ExpToLevelUp, progress.IsMaxLevel);
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
            EnsureGameOverImage();
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
            SetGameOverPanelActiveOnly(visible);

            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = visible;
        }

        private void SetGameOverPanelActiveOnly(bool visible)
        {
            if (gameOverPanel != null)
            {
                EnsureGameOverImage();
                gameOverPanel.SetActive(visible);
                SetGameOverImageVisible(visible);
                PlayGameOverAnimations(visible);
                return;
            }

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(visible);
            }
        }

        private void EnsureGameOverImage()
        {
            if (gameOverImage != null || gameOverPanel == null || string.IsNullOrWhiteSpace(gameOverImageChildName))
            {
                return;
            }

            var imageTransform = TransformSearchUtility.FindChildByName(gameOverPanel.transform, gameOverImageChildName);
            if (imageTransform != null)
            {
                gameOverImage = imageTransform.gameObject;
            }
        }

        private void SetGameOverImageVisible(bool visible)
        {
            if (gameOverImage != null)
            {
                gameOverImage.SetActive(visible);
            }
        }

        private void PlayGameOverAnimations(bool visible)
        {
            if (!visible)
            {
                return;
            }

            PlayAnimatorFromStart(gameOverImage != null ? gameOverImage.GetComponent<Animator>() : null);
            PlayAnimatorFromStart(restartButton != null ? restartButton.GetComponent<Animator>() : null);
        }

        private static void PlayAnimatorFromStart(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            animator.Rebind();
            animator.Update(0f);
            animator.Play(0, 0, 0f);
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
