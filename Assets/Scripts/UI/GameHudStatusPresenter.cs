using DinoGrow.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DinoGrow.UI
{
    public sealed class GameHudStatusPresenter
    {
        private readonly Text statusText;
        private StatusTextView statusTextView;

        public GameHudStatusPresenter(Text statusText, StatusTextView statusTextView)
        {
            this.statusText = statusText;
            this.statusTextView = statusTextView;
            EnsureStatusTextView();
        }

        public void SetText(string value)
        {
            EnsureStatusTextView();
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
    }
}
