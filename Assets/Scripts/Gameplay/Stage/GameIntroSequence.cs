using DinoGrow.Core.Stage;
using DinoGrow.Camera;
using DinoGrow.Infrastructure.Events;
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

        private GameStateController gameState;
        private GameEventBus eventBus;
        private bool started;
        private bool subscribedToEvents;

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
                    StartGame();
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
            StartGame();
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
