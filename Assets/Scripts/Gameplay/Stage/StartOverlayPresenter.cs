using System.Collections;
using UnityEngine;

namespace DinoGrow.Gameplay.Stage
{
    internal sealed class StartOverlayPresenter
    {
        private readonly MonoBehaviour owner;
        private GameObject panel;
        private GameObject firstText;
        private GameObject secondText;
        private float fadeDuration;
        private AudioClip roarClip;
        private AudioSource roarSource;
        private float roarVolume;
        private CanvasGroup panelGroup;
        private CanvasGroup firstTextGroup;
        private CanvasGroup secondTextGroup;

        public StartOverlayPresenter(MonoBehaviour owner)
        {
            this.owner = owner;
        }

        public void Configure(
            GameObject panel,
            GameObject firstText,
            GameObject secondText,
            float fadeDuration,
            AudioClip roarClip,
            AudioSource roarSource,
            float roarVolume)
        {
            this.panel = panel;
            this.firstText = firstText;
            this.secondText = secondText;
            this.fadeDuration = fadeDuration;
            this.roarClip = roarClip;
            if (roarSource != null)
            {
                this.roarSource = roarSource;
            }

            this.roarVolume = Mathf.Clamp01(roarVolume);
        }

        public void HideImmediate()
        {
            CacheCanvasGroups();
            SetVisible(false, false, false);
            SetAlpha(0f, 0f, 0f);
        }

        public IEnumerator PlaySequence(float firstTextHoldDuration, float secondTextHoldDuration, bool showSecondText)
        {
            CacheCanvasGroups();
            SetAlpha(0f, 0f, 0f);

            SetVisible(true, true, false);
            yield return Fade(0f, 1f, true, false);
            yield return WaitForSeconds(firstTextHoldDuration);
            yield return Fade(1f, 0f, true, false);

            if (showSecondText && secondText != null)
            {
                SetVisible(true, false, true);
                yield return Fade(0f, 1f, false, true);
                yield return WaitForSeconds(secondTextHoldDuration);
                PlayRoar();
            }

            HideImmediate();
        }

        public IEnumerator PlaySingle(float holdDuration, bool useSecondText)
        {
            CacheCanvasGroups();
            SetAlpha(0f, 0f, 0f);

            SetVisible(true, !useSecondText, useSecondText);
            yield return Fade(0f, 1f, !useSecondText, useSecondText);
            yield return WaitForSeconds(holdDuration);
            if (useSecondText)
            {
                PlayRoar();
            }

            yield return Fade(1f, 0f, !useSecondText, useSecondText);
            HideImmediate();
        }

        private IEnumerator Fade(float from, float to, bool showFirstText, bool showSecondText)
        {
            var duration = Mathf.Max(0f, fadeDuration);
            if (duration <= 0f)
            {
                SetAlpha(to, showFirstText ? to : 0f, showSecondText ? to : 0f);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var alpha = Mathf.Lerp(from, to, progress);
                SetAlpha(alpha, showFirstText ? alpha : 0f, showSecondText ? alpha : 0f);
                yield return null;
            }

            SetAlpha(to, showFirstText ? to : 0f, showSecondText ? to : 0f);
        }

        private static IEnumerator WaitForSeconds(float seconds)
        {
            var delay = Mathf.Max(0f, seconds);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
        }

        private void SetVisible(bool panelVisible, bool firstTextVisible, bool secondTextVisible)
        {
            if (panel != null)
            {
                panel.SetActive(panelVisible);
            }

            if (firstText != null)
            {
                firstText.SetActive(firstTextVisible);
            }

            if (secondText != null)
            {
                secondText.SetActive(secondTextVisible);
            }
        }

        private void CacheCanvasGroups()
        {
            panelGroup = GetOrAddCanvasGroup(panel);
            firstTextGroup = GetOrAddCanvasGroup(firstText);
            secondTextGroup = GetOrAddCanvasGroup(secondText);
        }

        private void SetAlpha(float panelAlpha, float firstTextAlpha, float secondTextAlpha)
        {
            SetCanvasGroupAlpha(panelGroup, panelAlpha);
            SetCanvasGroupAlpha(firstTextGroup, firstTextAlpha);
            SetCanvasGroupAlpha(secondTextGroup, secondTextAlpha);
        }

        private void PlayRoar()
        {
            if (roarClip == null || owner == null)
            {
                return;
            }

            if (roarSource == null)
            {
                roarSource = owner.gameObject.AddComponent<AudioSource>();
            }

            roarSource.playOnAwake = false;
            roarSource.loop = false;
            roarSource.spatialBlend = 0f;
            roarSource.volume = roarVolume;
            roarSource.PlayOneShot(roarClip, roarVolume);
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            if (target.TryGetComponent<CanvasGroup>(out var group))
            {
                return group;
            }

            return target.AddComponent<CanvasGroup>();
        }

        private static void SetCanvasGroupAlpha(CanvasGroup group, float alpha)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = Mathf.Clamp01(alpha);
        }
    }
}
