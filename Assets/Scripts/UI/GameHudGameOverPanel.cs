using DinoGrow.Core.Stage;
using DinoGrow.Infrastructure.Events;
using TMPro;
using UnityEngine;
using VContainer;

public class GameHudGameOverPanel : MonoBehaviour
{
    [Header("Game Over Background")]
    [Tooltip("Background object shown while the game over panel is visible.")]
    [SerializeField] private GameObject gameOverBackground;

    [Header("Game Over Text")]
    [Tooltip("Main GAME OVER text.")]
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("Game Over Subtitle")]
    [Tooltip("Subtitle shown below the main game over text.")]
    [SerializeField] private TextMeshProUGUI gameOverSubtitleText;

    [Header("Editor Preview")]
    [Tooltip("Shows the panel immediately when entering play mode for testing.")]
    [SerializeField] private bool showOnStart;

    private GameEventBus eventBus;
    private bool subscribedToEvents;

    [Inject]
    public void Construct(GameEventBus eventBus)
    {
        this.eventBus = eventBus;
        SubscribeToEvents();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            ApplyEditModePreview();
            return;
        }

        SubscribeToEvents();
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            ApplyEditModePreview();
            return;
        }

        SetVisible(showOnStart);
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        UnsubscribeFromEvents();
    }

    private void OnValidate()
    {
        ApplyEditModePreview();
    }

    public void ShowGameOver()
    {
        SetVisible(true);
    }

    public void HideGameOver()
    {
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (gameOverBackground != null)
        {
            gameOverBackground.SetActive(visible);
        }

        if (gameOverText != null)
        {
            ConfigureGameOverText();
            gameOverText.gameObject.SetActive(visible);
        }

        if (gameOverSubtitleText != null)
        {
            ConfigureGameOverSubtitleText();
            gameOverSubtitleText.gameObject.SetActive(visible);
        }
    }

    private void ConfigureGameOverText()
    {
        if (gameOverText == null)
        {
            return;
        }

        gameOverText.textWrappingMode = TextWrappingModes.NoWrap;
        gameOverText.overflowMode = TextOverflowModes.Overflow;
    }

    private void ConfigureGameOverSubtitleText()
    {
        if (gameOverSubtitleText == null)
        {
            return;
        }

        gameOverSubtitleText.textWrappingMode = TextWrappingModes.NoWrap;
        gameOverSubtitleText.overflowMode = TextOverflowModes.Overflow;
    }

    private void SubscribeToEvents()
    {
        if (eventBus == null || subscribedToEvents)
        {
            return;
        }

        eventBus.GameStateChanged += OnGameStateChanged;
        subscribedToEvents = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (eventBus == null || !subscribedToEvents)
        {
            return;
        }

        eventBus.GameStateChanged -= OnGameStateChanged;
        subscribedToEvents = false;
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
        {
            ShowGameOver();
            return;
        }

        HideGameOver();
    }

    private void ApplyEditModePreview()
    {
        if (Application.isPlaying)
        {
            return;
        }

        SetVisible(showOnStart);
    }
}
