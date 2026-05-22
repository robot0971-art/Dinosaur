using UnityEngine;
using UnityEngine.UI;

namespace DinoGrow.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(Text))]
    public sealed class StatusTextView : MonoBehaviour
    {
        [SerializeField] private Text targetText;

        private void Reset()
        {
            targetText = GetComponent<Text>();
        }

        private void Awake()
        {
            EnsureTargetText();
        }

        private void OnValidate()
        {
            EnsureTargetText();
        }

        public void SetText(string value)
        {
            EnsureTargetText();
            if (targetText == null)
            {
                return;
            }

            targetText.text = value;
            targetText.enabled = !string.IsNullOrEmpty(value);
        }

        public void Clear()
        {
            SetText(string.Empty);
        }

        private void EnsureTargetText()
        {
            if (targetText == null)
            {
                targetText = GetComponent<Text>();
            }
        }
    }
}
