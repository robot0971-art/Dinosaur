using DinoGrow.Core.Stage;
using DinoGrow.Camera;
using DinoGrow.Infrastructure.Events;
using System;
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
        [Header("Stage Transition Overlay")]
        [SerializeField] private float stageStartTextHoldDuration = 1.9f;
        [Header("Stage Start Sound")]
        [SerializeField] private AudioClip stageStartRoarClip;
        [SerializeField] private AudioSource stageStartRoarSource;
        [SerializeField, Range(0f, 1f)] private float stageStartRoarVolume = 1f;

        private GameStateController gameState;
        private GameEventBus eventBus;
        private bool started;
        private Coroutine startOverlayRoutine;
        private bool subscribedToEvents;
        private StartOverlayPresenter startOverlayPresenter;

        public event Action PresentationStarting;

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

            GetStartOverlayPresenter().HideImmediate();
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

            NotifyPresentationStarting();
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
                NotifyPresentationStarting();
                StartGame();
                return;
            }

            if (startOverlayRoutine != null)
            {
                StopCoroutine(startOverlayRoutine);
            }

            NotifyPresentationStarting();
            startOverlayRoutine = StartCoroutine(StartOverlayRoutine());
        }

        public IEnumerator PlayStageTransitionStartOverlay()
        {
            if (startPanel == null)
            {
                yield break;
            }

            if (startOverlayRoutine != null)
            {
                StopCoroutine(startOverlayRoutine);
                startOverlayRoutine = null;
            }

            NotifyPresentationStarting();
            yield return PlaySingleStartOverlay(
                Mathf.Max(0f, stageStartTextHoldDuration),
                startText2 != null);
        }

        private void NotifyPresentationStarting()
        {
            PresentationStarting?.Invoke();
        }

        private IEnumerator StartOverlayRoutine()
        {
            yield return PlayStartOverlaySequence(
                Mathf.Max(0f, startTextHoldDuration),
                Mathf.Max(0f, startText2HoldDuration),
                true);
            startOverlayRoutine = null;
            StartGame();
        }

        private IEnumerator PlayStartOverlaySequence(float firstTextHoldDuration, float secondTextHoldDuration, bool showSecondText)
        {
            yield return GetStartOverlayPresenter().PlaySequence(
                firstTextHoldDuration,
                secondTextHoldDuration,
                showSecondText);
        }

        private IEnumerator PlaySingleStartOverlay(float holdDuration, bool useSecondText)
        {
            yield return GetStartOverlayPresenter().PlaySingle(holdDuration, useSecondText);
        }

        private StartOverlayPresenter GetStartOverlayPresenter()
        {
            startOverlayPresenter ??= new StartOverlayPresenter(this);
            startOverlayPresenter.Configure(
                startPanel,
                startText,
                startText2,
                startOverlayFadeDuration,
                stageStartRoarClip,
                stageStartRoarSource,
                stageStartRoarVolume);
            return startOverlayPresenter;
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
