using DinoGrow.Core.Growth;
using UnityEngine.UI;

namespace DinoGrow.UI
{
    public sealed class GameHudProgressPresenter
    {
        private readonly Text levelText;
        private readonly Text expText;
        private readonly global::GameHudLevelExpPanel levelExpPanel;

        public GameHudProgressPresenter(
            Text levelText,
            Text expText,
            global::GameHudLevelExpPanel levelExpPanel)
        {
            this.levelText = levelText;
            this.expText = expText;
            this.levelExpPanel = levelExpPanel;
        }

        public void Refresh(PlayerProgress progress)
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
    }
}
