using DinoGrow.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DinoGrow.UI
{
    public sealed class GameHudGameOverPresenter
    {
        private readonly GameObject gameOverPanel;
        private readonly Button restartButton;
        private readonly string gameOverImageChildName;
        private readonly AudioClip gameOverSoundClip;
        private readonly float gameOverSoundVolume;
        private readonly GameObject audioSourceOwner;
        private GameObject gameOverImage;
        private AudioSource gameOverSoundSource;

        public GameHudGameOverPresenter(
            GameObject gameOverPanel,
            Button restartButton,
            GameObject gameOverImage,
            string gameOverImageChildName,
            AudioClip gameOverSoundClip,
            AudioSource gameOverSoundSource,
            float gameOverSoundVolume,
            GameObject audioSourceOwner)
        {
            this.gameOverPanel = gameOverPanel;
            this.restartButton = restartButton;
            this.gameOverImage = gameOverImage;
            this.gameOverImageChildName = gameOverImageChildName;
            this.gameOverSoundClip = gameOverSoundClip;
            this.gameOverSoundSource = gameOverSoundSource;
            this.gameOverSoundVolume = Mathf.Clamp01(gameOverSoundVolume);
            this.audioSourceOwner = audioSourceOwner;
        }

        public GameObject GameOverImage => gameOverImage;

        public void Show(bool visible, bool updateCursor)
        {
            SetActiveOnly(visible);
            if (!updateCursor)
            {
                return;
            }

            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = visible;
        }

        public void SetActiveOnly(bool visible)
        {
            EnsureGameOverImage();
            if (gameOverPanel != null)
            {
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

        public void PlaySound()
        {
            if (!Application.isPlaying || gameOverSoundClip == null)
            {
                return;
            }

            if (gameOverSoundSource == null && audioSourceOwner != null)
            {
                gameOverSoundSource = audioSourceOwner.AddComponent<AudioSource>();
            }

            if (gameOverSoundSource == null)
            {
                return;
            }

            gameOverSoundSource.playOnAwake = false;
            gameOverSoundSource.loop = false;
            gameOverSoundSource.spatialBlend = 0f;
            gameOverSoundSource.volume = gameOverSoundVolume;
            gameOverSoundSource.PlayOneShot(gameOverSoundClip, gameOverSoundVolume);
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
    }
}
