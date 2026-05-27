using DinoGrow.Core.Stage;
using DinoGrow.Camera;
using DinoGrow.Infrastructure.Events;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using VContainer;

namespace DinoGrow.Gameplay.Stage
{
    [RequireComponent(typeof(PlayableDirector))]
    public sealed class GameIntroSequence : MonoBehaviour
    {
        [SerializeField] private PlayableDirector introDirector;
        [SerializeField] private CinemachineCamera playerCamera;
        [SerializeField] private CinemachineCamera introCamera;
        [SerializeField] private CinemachineThirdPersonOrbit playerCameraOrbit;
        [SerializeField] private bool playAfterInitialMapLoaded = true;
        [SerializeField] private bool startGameIfTimelineMissing = true;
        [Header("Start Overlay")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject startText;
        [SerializeField] private GameObject startText2;
        [SerializeField] private float startOverlayFadeDuration = 0.35f;
        [SerializeField] private float startTextHoldDuration = 3f;
        [SerializeField] private float startText2HoldDuration = 2f;

        private GameStateController gameState;
        private GameEventBus eventBus;
        private bool started;
        private Coroutine startOverlayRoutine;
        private bool subscribedToEvents;
        private CanvasGroup startPanelGroup;
        private CanvasGroup startTextGroup;
        private CanvasGroup startText2Group;

        [Inject]
        public void Construct(GameStateController gameState, GameEventBus eventBus)
        {
            this.gameState = gameState;
            this.eventBus = eventBus;
            SubscribeToEvents();
        }

        public void ConfigurePlayerCameraOrbit(CinemachineThirdPersonOrbit orbit)
        {
            playerCameraOrbit = orbit;

            if (playerCamera == null && orbit != null)
            {
                playerCamera = orbit.GetComponent<CinemachineCamera>();
            }
        }

        private void Awake()
        {
            if (introDirector == null)
            {
                introDirector = GetComponent<PlayableDirector>();
            }

            CachePlayerCameraOrbit();

            if (introDirector != null)
            {
                introDirector.playOnAwake = false;
                introDirector.extrapolationMode = DirectorWrapMode.None;
            }

            SetStartOverlayVisible(false, false, false);
            CacheStartOverlayCanvasGroups();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void Start()
        {
            if (!playAfterInitialMapLoaded)
            {
                PlayIntroOrStartGame();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            if (startOverlayRoutine != null)
            {
                StopCoroutine(startOverlayRoutine);
                startOverlayRoutine = null;
            }

            if (introDirector != null)
            {
                introDirector.stopped -= OnIntroStopped;
            }
        }

        private void OnInitialMapLoaded()
        {
            if (!playAfterInitialMapLoaded)
            {
                return;
            }

            PlayIntroOrStartGame();
        }

        private void PlayIntroOrStartGame()
        {
            if (started)
            {
                return;
            }

            started = true;

            if (introDirector == null || introDirector.playableAsset == null)
            {
                if (startGameIfTimelineMissing)
                {
                    RestorePlayerCamera();
                    PlayStartOverlayThenStartGame();
                }

                return;
            }

            introDirector.time = 0d;
            introDirector.stopped += OnIntroStopped;
            if (introCamera != null)
            {
                introCamera.gameObject.SetActive(true);
            }

            introDirector.Play();
        }

        private void OnIntroStopped(PlayableDirector director)
        {
            if (director != introDirector)
            {
                return;
            }

            introDirector.stopped -= OnIntroStopped;
            RestorePlayerCamera();
            PlayStartOverlayThenStartGame();
        }

        private void RestorePlayerCamera()
        {
            if (introDirector != null)
            {
                introDirector.extrapolationMode = DirectorWrapMode.None;
                introDirector.Stop();
            }

            CachePlayerCameraOrbit();

            if (introCamera != null)
            {
                introCamera.gameObject.SetActive(false);
            }

            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
            }

            playerCameraOrbit?.RecenterNow();
        }

        private void CachePlayerCameraOrbit()
        {
            if (playerCameraOrbit == null && playerCamera != null)
            {
                playerCameraOrbit = playerCamera.GetComponent<CinemachineThirdPersonOrbit>();
            }
        }

        private void StartGame()
        {
            if (gameState == null || eventBus == null || gameState.IsPlaying)
            {
                return;
            }

            gameState.StartGame();
            eventBus.PublishGameStateChanged(gameState.State);
        }

        private void PlayStartOverlayThenStartGame()
        {
            if (startPanel == null)
            {
                StartGame();
                return;
            }

            if (startOverlayRoutine != null)
            {
                StopCoroutine(startOverlayRoutine);
            }

            startOverlayRoutine = StartCoroutine(StartOverlayRoutine());
        }

        private IEnumerator StartOverlayRoutine()
        {
            CacheStartOverlayCanvasGroups();
            SetStartOverlayAlpha(0f, 0f, 0f);

            SetStartOverlayVisible(true, true, false);
            yield return FadeStartOverlay(0f, 1f, true, false);
            yield return WaitForStartOverlaySeconds(startTextHoldDuration);
            yield return FadeStartOverlay(1f, 0f, true, false);

            SetStartOverlayVisible(true, false, true);
            yield return FadeStartOverlay(0f, 1f, false, true);
            yield return WaitForStartOverlaySeconds(startText2HoldDuration);

            SetStartOverlayVisible(false, false, false);
            SetStartOverlayAlpha(0f, 0f, 0f);
            startOverlayRoutine = null;
            StartGame();
        }

        private IEnumerator FadeStartOverlay(float from, float to, bool showFirstText, bool showSecondText)
        {
            var duration = Mathf.Max(0f, startOverlayFadeDuration);
            if (duration <= 0f)
            {
                SetStartOverlayAlpha(to, showFirstText ? to : 0f, showSecondText ? to : 0f);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var alpha = Mathf.Lerp(from, to, progress);
                SetStartOverlayAlpha(alpha, showFirstText ? alpha : 0f, showSecondText ? alpha : 0f);
                yield return null;
            }

            SetStartOverlayAlpha(to, showFirstText ? to : 0f, showSecondText ? to : 0f);
        }

        private IEnumerator WaitForStartOverlaySeconds(float seconds)
        {
            var delay = Mathf.Max(0f, seconds);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
        }

        private void SetStartOverlayVisible(bool panelVisible, bool firstTextVisible, bool secondTextVisible)
        {
            if (startPanel != null)
            {
                startPanel.SetActive(panelVisible);
            }

            if (startText != null)
            {
                startText.SetActive(firstTextVisible);
            }

            if (startText2 != null)
            {
                startText2.SetActive(secondTextVisible);
            }
        }

        private void CacheStartOverlayCanvasGroups()
        {
            startPanelGroup = GetOrAddCanvasGroup(startPanel);
            startTextGroup = GetOrAddCanvasGroup(startText);
            startText2Group = GetOrAddCanvasGroup(startText2);
        }

        private void SetStartOverlayAlpha(float panelAlpha, float firstTextAlpha, float secondTextAlpha)
        {
            SetCanvasGroupAlpha(startPanelGroup, panelAlpha);
            SetCanvasGroupAlpha(startTextGroup, firstTextAlpha);
            SetCanvasGroupAlpha(startText2Group, secondTextAlpha);
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

        private void SubscribeToEvents()
        {
            if (eventBus == null || subscribedToEvents)
            {
                return;
            }

            eventBus.InitialMapLoaded += OnInitialMapLoaded;
            subscribedToEvents = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (eventBus == null || !subscribedToEvents)
            {
                return;
            }

            eventBus.InitialMapLoaded -= OnInitialMapLoaded;
            subscribedToEvents = false;
        }
    }
}
