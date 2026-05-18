using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;
using DinoGrow.Infrastructure.Events;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace DinoGrow.UI
{
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private Text levelText;
        [SerializeField] private Text expText;
        [SerializeField] private Text statusText;

        private PlayerProgress progress;
        private GameEventBus eventBus;

        [Inject]
        public void Construct(PlayerProgress progress, GameEventBus eventBus)
        {
            this.progress = progress;
            this.eventBus = eventBus;
        }

        private void OnEnable()
        {
            if (eventBus == null)
            {
                return;
            }

            eventBus.PlayerGrowthChanged += OnPlayerGrowthChanged;
            eventBus.GameStateChanged += OnGameStateChanged;
        }

        private void Start()
        {
            Refresh();
            SetStatus("");
        }

        private void OnDisable()
        {
            if (eventBus == null)
            {
                return;
            }

            eventBus.PlayerGrowthChanged -= OnPlayerGrowthChanged;
            eventBus.GameStateChanged -= OnGameStateChanged;
        }

        private void OnPlayerGrowthChanged(GrowthResult result)
        {
            Refresh();
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                SetStatus("GAME OVER");
            }
            else if (state == GameState.Clear)
            {
                SetStatus("LEVEL 20 CLEAR");
            }
            else
            {
                SetStatus("");
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
            if (statusText != null)
            {
                statusText.horizontalOverflow = HorizontalWrapMode.Overflow;
                statusText.verticalOverflow = VerticalWrapMode.Overflow;
                statusText.text = value;
                statusText.enabled = !string.IsNullOrEmpty(value);
            }
        }
    }
}
