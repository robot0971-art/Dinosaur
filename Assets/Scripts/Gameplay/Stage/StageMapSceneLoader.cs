using System.Collections;
using System.Collections.Generic;
using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;
using DinoGrow.Camera;
using DinoGrow.Gameplay.Enemy;
using DinoGrow.Gameplay.Player;
using DinoGrow.Infrastructure.DI;
using DinoGrow.Infrastructure.Events;
using DinoGrow.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace DinoGrow.Gameplay.Stage
{
    [DefaultExecutionOrder(-1000)]
    public sealed class StageMapSceneLoader : MonoBehaviour
    {
        [SerializeField] private string[] mapScenePaths =
        {
            "Assets/Scenes/map4.unity",
            "Assets/Scenes/map7.unity",
            "Assets/Scenes/map10.unity"
        };

        [SerializeField] private bool loadInitialMap = true;
        [SerializeField] private bool switchMapOnLevelUp = true;
        [SerializeField] private bool avoidImmediateRepeat = true;
        [SerializeField] private bool disableExistingSceneMaps = true;
        [SerializeField] private bool disableExistingSceneNavMeshSurfaces = true;
        [SerializeField] private bool movePlayerToMapStart = true;
        [SerializeField] private LayerMask mapGroundLayers = 0;
        [SerializeField] private float groundProbeHeight = 80f;
        [SerializeField] private float groundProbeDistance = 180f;
        [SerializeField] private float playerStartHeightOffset = 2.6f;
        [SerializeField] private string mapBoundaryRootName = "MapBoundary";
        [SerializeField] private float mapBoundaryInset = 8f;
        [SerializeField] private GameObject loadingOverlayPanel;
        [SerializeField] private Slider loadingSlider;
        [SerializeField] private bool logLoadingOverlayDiagnostics;
        [SerializeField] private GameHud gameHud;
        [SerializeField] private GameHudHeartUI heartUI;
        [SerializeField] private CinemachineThirdPersonOrbit cameraOrbit;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private GameIntroSequence startOverlaySequence;
        [SerializeField] private float initialLoadingMinVisibleDuration = 1.25f;
        [SerializeField] private int initialLoadingHideDelayFrames = 1;
        [SerializeField] private float stageTransitionIdleDelay = 0.85f;
        [SerializeField] private float stageTransitionFadeDuration = 2f;
        [SerializeField] private AudioClip stageClearSoundClip;
        [SerializeField] private AudioSource stageClearSoundSource;
        [SerializeField, Range(0f, 1f)] private float stageClearSoundVolume = 1f;
        [SerializeField] private AudioClip backgroundMusicClip;
        [SerializeField] private AudioSource backgroundMusicSource;
        [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.7f;

        private GameStateController gameState;
        private GameEventBus eventBus;
        private PlayerDinoController player;
        private CameraReference cameraReference;
        private string loadedMapScenePath;
        private bool isSwitching;
        private bool disabledInitialSceneMaps;
        private bool initialMapLoaded;
        private GameObject runtimeLoadingCurtain;
        private Slider runtimeLoadingSlider;
        private CanvasGroup runtimeLoadingCurtainGroup;
        private CanvasGroup loadingOverlayGroup;
        private Canvas loadingOverlayCanvas;
        private GameObject levelExpPanelObject;
        private GameObject heartRootObject;
        private bool levelExpPanelWasActive = true;
        private bool heartRootWasActive = true;
        private bool initialIntroPresentationStarted;

        private void Awake()
        {
            UseGroundLayerIfAvailable();
            if (loadInitialMap)
            {
                DisableExistingSceneMapRoots();
                disabledInitialSceneMaps = true;
            }
            else
            {
                SetLoadingProgress(0f, false);
            }
        }

        [Inject]
        public void Construct(
            GameEventBus eventBus,
            PlayerDinoController player,
            CameraReference cameraReference,
            GameStateController gameState)
        {
            this.eventBus = eventBus;
            this.player = player;
            this.cameraReference = cameraReference;
            this.gameState = gameState;
        }

        public void ConfigureLoadingOverlay(GameObject panel, Slider slider)
        {
            loadingOverlayPanel = panel;
            loadingSlider = slider;
            PrepareLoadingOverlayPanel();
            SetLoadingProgress(0f, loadInitialMap && !initialMapLoaded);
        }

        public void ConfigureHudVisibilityTargets(GameHud hud, GameHudHeartUI hearts)
        {
            gameHud = hud;
            heartUI = hearts;
            CacheHudVisibilityTargets();
            SetLoadingHudVisibility(loadInitialMap && !initialMapLoaded);
        }

        public void ConfigureCameraOrbit(CinemachineThirdPersonOrbit orbit)
        {
            cameraOrbit = orbit;
        }

        public void ConfigureEnemySpawner(EnemySpawner spawner)
        {
            enemySpawner = spawner;
        }

        public void ConfigureStageClearSound(AudioClip clip, AudioSource source, float volume)
        {
            stageClearSoundClip = clip;
            stageClearSoundSource = source;
            stageClearSoundVolume = Mathf.Clamp01(volume);
        }

        public void ConfigureBackgroundMusic(AudioClip clip, AudioSource source, float volume)
        {
            backgroundMusicClip = clip;
            backgroundMusicSource = source;
            backgroundMusicVolume = Mathf.Clamp01(volume);
            ConfigureBackgroundMusicSource();
        }

        public void ConfigureStartOverlaySequence(GameIntroSequence sequence)
        {
            if (startOverlaySequence != null)
            {
                startOverlaySequence.PresentationStarting -= OnIntroPresentationStarting;
            }

            startOverlaySequence = sequence;

            if (startOverlaySequence != null)
            {
                startOverlaySequence.PresentationStarting += OnIntroPresentationStarting;
            }
        }

        private void UseGroundLayerIfAvailable()
        {
            if (mapGroundLayers.value != 0)
            {
                return;
            }

            var groundLayer = LayerMask.NameToLayer("Ground");
            mapGroundLayers = groundLayer >= 0
                ? 1 << groundLayer
                : Physics.DefaultRaycastLayers;
        }

        private void Start()
        {
            if (eventBus != null)
            {
                eventBus.PlayerGrowthChanged += OnPlayerGrowthChanged;
            }

            if (loadInitialMap)
            {
                enemySpawner?.SetMapTransitionInProgress(true);
                if (!disabledInitialSceneMaps)
                {
                    DisableExistingSceneMapRoots();
                    disabledInitialSceneMaps = true;
                }

                StartCoroutine(LoadInitialRandomMapRoutine());
            }
        }

        private void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.PlayerGrowthChanged -= OnPlayerGrowthChanged;
            }

            if (startOverlaySequence != null)
            {
                startOverlaySequence.PresentationStarting -= OnIntroPresentationStarting;
            }

            if (runtimeLoadingCurtain != null)
            {
                Destroy(runtimeLoadingCurtain);
            }
        }

        private void OnPlayerGrowthChanged(GrowthResult result)
        {
            if (!switchMapOnLevelUp || !result.LeveledUp)
            {
                return;
            }

            SwitchToRandomMap();
        }

        private void SwitchToRandomMap()
        {
            var nextScenePath = PickRandomMapScenePath();
            if (string.IsNullOrWhiteSpace(nextScenePath))
            {
                return;
            }

            if (isSwitching)
            {
                StopAllCoroutines();
                isSwitching = false;
            }

            enemySpawner?.SetMapTransitionInProgress(true);
            StartCoroutine(SwitchMapRoutine(nextScenePath));
        }

        private IEnumerator LoadInitialRandomMapRoutine()
        {
            var nextScenePath = PickInitialRandomMapScenePath();
            if (string.IsNullOrWhiteSpace(nextScenePath))
            {
                enemySpawner?.SetMapTransitionInProgress(false);
                yield break;
            }

            SetLoadingProgress(0f, true);
            var loadingStartedAt = Time.realtimeSinceStartup;
            yield return null;

            var nextScene = SceneManager.GetSceneByPath(nextScenePath);
            if (!nextScene.IsValid() || !nextScene.isLoaded)
            {
                var loadOperation = SceneManager.LoadSceneAsync(nextScenePath, LoadSceneMode.Additive);
                yield return TrackLoadingOperation(loadOperation, 0f, 0.85f);
                nextScene = SceneManager.GetSceneByPath(nextScenePath);
            }

            SetLoadingProgress(0.9f, true);
            DisableMapSceneCameras(nextScene);
            ConfigureMapBillboards(nextScene);
            ApplyMapEnvironment(nextScene);
            ConfigurePlayerStartExclusion(nextScene);
            MovePlayerToStartPoint(nextScene);
            ApplyMapBoundary(nextScene, false);
            if (enemySpawner != null)
            {
                yield return enemySpawner.RebuildSpawnedEnemiesForMapTransition();
            }
            yield return null;
            ApplyMapEnvironment(nextScene);
            loadedMapScenePath = nextScenePath;
            yield return null;
            SetLoadingProgress(1f, true);
            yield return null;
            yield return WaitForMinimumInitialLoadingTime(loadingStartedAt);
            initialIntroPresentationStarted = false;
            eventBus?.PublishInitialMapLoaded();
            initialMapLoaded = true;
            if (startOverlaySequence != null)
            {
                yield return WaitForInitialIntroPresentationStart();
            }

            yield return WaitForLoadingHideDelayFrames();
            SetLoadingProgress(0f, false);
            DestroyRuntimeLoadingCurtain();
            enemySpawner?.SetMapTransitionInProgress(false);
        }

        private IEnumerator SwitchMapRoutine(string nextScenePath)
        {
            isSwitching = true;
            PauseGameplayForStageTransition();
            if (stageTransitionIdleDelay > 0f)
            {
                yield return new WaitForSeconds(stageTransitionIdleDelay);
            }

            PlayStageClearSound();
            yield return FadeLoadingOverlay(true, stageTransitionFadeDuration);
            SetLoadingOverlayAlpha(1f);
            yield return null;

            if (!string.IsNullOrWhiteSpace(loadedMapScenePath))
            {
                var loadedScene = SceneManager.GetSceneByPath(loadedMapScenePath);
                if (loadedScene.IsValid() && loadedScene.isLoaded)
                {
                    var unloadOperation = SceneManager.UnloadSceneAsync(loadedScene);
                    yield return TrackLoadingOperation(unloadOperation, 0f, 0.35f);
                }
            }

            var nextScene = SceneManager.GetSceneByPath(nextScenePath);
            if (!nextScene.IsValid() || !nextScene.isLoaded)
            {
                var loadOperation = SceneManager.LoadSceneAsync(nextScenePath, LoadSceneMode.Additive);
                yield return TrackLoadingOperation(loadOperation, 0.35f, 0.9f);
                nextScene = SceneManager.GetSceneByPath(nextScenePath);
            }

            SetLoadingProgress(0.95f, true);
            DisableMapSceneCameras(nextScene);
            ConfigureMapBillboards(nextScene);
            ApplyMapEnvironment(nextScene);
            ConfigurePlayerStartExclusion(nextScene);
            MovePlayerToStartPoint(nextScene);
            ApplyMapBoundary(nextScene);
            yield return null;
            ApplyMapEnvironment(nextScene);
            loadedMapScenePath = nextScenePath;
            yield return null;
            SetLoadingProgress(1f, true);
            yield return null;
            if (startOverlaySequence != null)
            {
                var overlayRoutine = StartCoroutine(startOverlaySequence.PlayStageTransitionStartOverlay());
                yield return FadeLoadingOverlay(false, stageTransitionFadeDuration);
                yield return overlayRoutine;
            }
            else
            {
                yield return FadeLoadingOverlay(false, stageTransitionFadeDuration);
            }

            enemySpawner?.SetMapTransitionInProgress(false);
            ResumeGameplayAfterStageTransition();
            isSwitching = false;
        }

        private IEnumerator WaitForInitialIntroPresentationStart()
        {
            while (!initialIntroPresentationStarted)
            {
                yield return null;
            }
        }

        private IEnumerator WaitForLoadingHideDelayFrames()
        {
            var frameCount = Mathf.Max(0, initialLoadingHideDelayFrames);
            for (var i = 0; i < frameCount; i++)
            {
                yield return null;
            }
        }

        private void OnIntroPresentationStarting()
        {
            initialIntroPresentationStarted = true;
        }

        private IEnumerator WaitForMinimumInitialLoadingTime(float loadingStartedAt)
        {
            var remaining = Mathf.Max(0f, initialLoadingMinVisibleDuration - (Time.realtimeSinceStartup - loadingStartedAt));
            if (remaining > 0f)
            {
                yield return new WaitForSecondsRealtime(remaining);
            }
        }

        private void PauseGameplayForStageTransition()
        {
            if (gameState == null || eventBus == null)
            {
                return;
            }

            gameState.Reset();
            eventBus.PublishGameStateChanged(gameState.State);
        }

        private void ResumeGameplayAfterStageTransition()
        {
            if (gameState == null || eventBus == null || gameState.IsPlaying)
            {
                return;
            }

            gameState.StartGame();
            eventBus.PublishGameStateChanged(gameState.State);
        }

        private void PlayStageClearSound()
        {
            if (stageClearSoundClip == null)
            {
                return;
            }

            if (stageClearSoundSource == null)
            {
                stageClearSoundSource = gameObject.AddComponent<AudioSource>();
            }

            stageClearSoundSource.playOnAwake = false;
            stageClearSoundSource.loop = false;
            stageClearSoundSource.spatialBlend = 0f;
            stageClearSoundSource.volume = stageClearSoundVolume;
            stageClearSoundSource.PlayOneShot(stageClearSoundClip, stageClearSoundVolume);
        }

        private void UpdateBackgroundMusicForLoading(bool loadingVisible)
        {
            if (!Application.isPlaying || backgroundMusicClip == null)
            {
                return;
            }

            ConfigureBackgroundMusicSource();
            if (backgroundMusicSource == null)
            {
                return;
            }

            if (loadingVisible)
            {
                if (backgroundMusicSource.isPlaying)
                {
                    backgroundMusicSource.Pause();
                }

                return;
            }

            backgroundMusicSource.volume = backgroundMusicVolume;
            if (!backgroundMusicSource.isPlaying)
            {
                backgroundMusicSource.Play();
            }
        }

        private void ConfigureBackgroundMusicSource()
        {
            if (backgroundMusicClip == null)
            {
                return;
            }

            if (backgroundMusicSource == null)
            {
                backgroundMusicSource = gameObject.AddComponent<AudioSource>();
            }

            backgroundMusicSource.clip = backgroundMusicClip;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.playOnAwake = false;
            backgroundMusicSource.spatialBlend = 0f;
            backgroundMusicSource.volume = backgroundMusicVolume;
        }

        private IEnumerator TrackLoadingOperation(AsyncOperation operation, float startProgress, float endProgress)
        {
            if (operation == null)
            {
                SetLoadingProgress(endProgress, true);
                yield break;
            }

            while (!operation.isDone)
            {
                SetLoadingProgress(Mathf.Lerp(startProgress, endProgress, Mathf.Clamp01(operation.progress / 0.9f)), true);
                yield return null;
            }

            SetLoadingProgress(endProgress, true);
        }

        private void SetLoadingProgress(float progress, bool visible)
        {
            if (logLoadingOverlayDiagnostics)
            {
                Debug.Log(
                    $"[LoadingOverlay] {nameof(StageMapSceneLoader)}.{nameof(SetLoadingProgress)} " +
                    $"progress={Mathf.Clamp01(progress):0.###}, visible={visible}, " +
                    $"panel={(loadingOverlayPanel != null ? loadingOverlayPanel.name : "null")}, " +
                    $"panelActive={(loadingOverlayPanel != null ? loadingOverlayPanel.activeInHierarchy.ToString() : "null")}, " +
                    $"slider={(loadingSlider != null ? loadingSlider.name : "null")}",
                    this);
            }

            SetLoadingHudVisibility(visible);
            UpdateBackgroundMusicForLoading(visible);
            var useRuntimeCurtain = ShouldUseRuntimeLoadingCurtain(visible);

            if (runtimeLoadingCurtain != null)
            {
                runtimeLoadingCurtain.SetActive(useRuntimeCurtain);
            }

            if (runtimeLoadingSlider != null)
            {
                runtimeLoadingSlider.value = Mathf.Clamp01(progress);
            }

            if (loadingOverlayPanel != null)
            {
                PrepareLoadingOverlayPanel();
                loadingOverlayPanel.SetActive(visible);
            }

            if (loadingSlider != null)
            {
                loadingSlider.value = Mathf.Clamp01(progress);
            }
        }

        private void SetLoadingHudVisibility(bool loadingVisible)
        {
            // Keep the existing loading panel reliable first. HUD hiding can be restored
            // later with explicitly assigned targets that do not include the loading canvas.
        }

        private void CacheHudVisibilityTargets()
        {
            if (levelExpPanelObject == null && gameHud != null)
            {
                var levelExpPanel = gameHud.GetComponentInChildren<global::GameHudLevelExpPanel>(true);
                if (levelExpPanel != null)
                {
                    levelExpPanelObject = levelExpPanel.gameObject;
                    levelExpPanelWasActive = loadInitialMap && !initialMapLoaded
                        ? true
                        : levelExpPanelObject.activeSelf;
                }
            }

            if (heartRootObject == null && heartUI != null)
            {
                heartRootObject = heartUI.gameObject;
                heartRootWasActive = loadInitialMap && !initialMapLoaded
                    ? true
                    : heartRootObject.activeSelf;
            }
        }

        private bool ShouldUseRuntimeLoadingCurtain(bool visible)
        {
            if (!visible)
            {
                return false;
            }

            if (loadingOverlayPanel != null)
            {
                if (runtimeLoadingCurtain != null)
                {
                    runtimeLoadingCurtain.SetActive(false);
                }

                return false;
            }

            Debug.LogWarning($"{nameof(StageMapSceneLoader)} needs a custom loading overlay panel. Runtime loading bars are disabled.", this);
            return false;
        }

        private static void SetHudTargetVisible(ref GameObject target, ref bool wasActive, bool visible)
        {
            if (target == null)
            {
                return;
            }

            if (!visible)
            {
                wasActive = target.activeSelf;
                target.SetActive(false);
                return;
            }

            target.SetActive(wasActive);
        }

        private IEnumerator FadeLoadingOverlay(bool visible, float duration)
        {
            CacheLoadingOverlayGroups();
            SetLoadingProgress(visible ? 1f : 0f, true);

            var from = visible ? 0f : 1f;
            var to = visible ? 1f : 0f;
            duration = Mathf.Max(0f, duration);
            if (duration <= 0f)
            {
                SetLoadingOverlayAlpha(to);
                SetLoadingProgress(visible ? 1f : 0f, visible);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                SetLoadingOverlayAlpha(Mathf.Lerp(from, to, progress));
                yield return null;
            }

            SetLoadingOverlayAlpha(to);
            SetLoadingProgress(visible ? 1f : 0f, visible);
        }

        private void CacheLoadingOverlayGroups()
        {
            runtimeLoadingCurtainGroup = GetOrAddCanvasGroup(runtimeLoadingCurtain);
            loadingOverlayGroup = GetOrAddCanvasGroup(loadingOverlayPanel);
        }

        private void SetLoadingOverlayAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            SetCanvasGroupAlpha(runtimeLoadingCurtainGroup, alpha);
            SetCanvasGroupAlpha(loadingOverlayGroup, alpha);
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
            if (group != null)
            {
                group.alpha = alpha;
            }
        }

        private void PrepareLoadingOverlayPanel()
        {
            if (loadingOverlayPanel == null)
            {
                return;
            }

            loadingOverlayPanel.transform.SetAsLastSibling();

            loadingOverlayCanvas = loadingOverlayPanel.GetComponent<Canvas>();
            if (loadingOverlayCanvas == null)
            {
                loadingOverlayCanvas = loadingOverlayPanel.AddComponent<Canvas>();
            }

            loadingOverlayCanvas.overrideSorting = true;
            loadingOverlayCanvas.sortingOrder = short.MaxValue;

            loadingOverlayGroup = GetOrAddCanvasGroup(loadingOverlayPanel);
            SetCanvasGroupAlpha(loadingOverlayGroup, 1f);
        }

        private static void DisableMapSceneCameras(Scene mapScene)
        {
            if (!mapScene.IsValid() || !mapScene.isLoaded)
            {
                return;
            }

            foreach (var root in mapScene.GetRootGameObjects())
            {
                foreach (var targetCamera in root.GetComponentsInChildren<UnityEngine.Camera>(true))
                {
                    targetCamera.enabled = false;
                }

                foreach (var listener in root.GetComponentsInChildren<AudioListener>(true))
                {
                    listener.enabled = false;
                }
            }
        }

        private static void ApplyMapEnvironment(Scene mapScene)
        {
            if (!mapScene.IsValid() || !mapScene.isLoaded)
            {
                return;
            }

            foreach (var root in mapScene.GetRootGameObjects())
            {
                var environment = root.GetComponentInChildren<EnvironmentSettingsController>(true);
                if (environment == null)
                {
                    continue;
                }

                environment.Apply();
                return;
            }
        }

        private void ConfigureMapBillboards(Scene mapScene)
        {
            if (cameraReference?.Transform == null || !mapScene.IsValid() || !mapScene.isLoaded)
            {
                return;
            }

            foreach (var root in mapScene.GetRootGameObjects())
            {
                foreach (var billboard in root.GetComponentsInChildren<BillboardToCamera>(true))
                {
                    billboard.SetTarget(cameraReference.Transform);
                }
            }
        }

        private void DisableExistingSceneMapRoots()
        {
            if (!disableExistingSceneMaps)
            {
                return;
            }

            var activeScene = gameObject.scene;
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                return;
            }

            foreach (var root in activeScene.GetRootGameObjects())
            {
                DisableExistingNavMeshSurfaces(root);

                if (LooksLikeMapRoot(root))
                {
                    root.SetActive(false);
                    continue;
                }

                if (ShouldKeepMainSceneRoot(root))
                {
                    continue;
                }
            }
        }

        private void DisableExistingNavMeshSurfaces(GameObject root)
        {
            if (!disableExistingSceneNavMeshSurfaces || root == null)
            {
                return;
            }

            foreach (var surface in root.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>(true))
            {
                surface.enabled = false;
            }
        }

        private bool ShouldKeepMainSceneRoot(GameObject root)
        {
            if (root == null || root == gameObject || root.GetComponentInChildren<PlayerDinoController>(true) != null)
            {
                return true;
            }

            return root.GetComponentInChildren<Canvas>(true) != null
                || root.GetComponentInChildren<EventSystem>(true) != null
                || root.GetComponentInChildren<UnityEngine.Camera>(true) != null
                || root.GetComponentInChildren<Light>(true) != null;
        }

        private bool LooksLikeMapRoot(GameObject root)
        {
            if (root == null)
            {
                return false;
            }

            if (root.GetComponentInChildren<PlayerDinoController>(true) != null
                || root.GetComponentInChildren<Canvas>(true) != null
                || root.GetComponentInChildren<EventSystem>(true) != null)
            {
                return false;
            }

            if (root.name.Contains("Map") || root.name.Contains("Ground") || root.name.Contains("Environment"))
            {
                return true;
            }

            if (root.GetComponentInChildren<EnvironmentSettingsController>(true) != null
                || root.GetComponentInChildren<Unity.AI.Navigation.NavMeshSurface>(true) != null
                || FindChildByName(root.transform, "PlayerStartPoint") != null
                || FindChildByName(root.transform, mapBoundaryRootName) != null)
            {
                return true;
            }

            var groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0)
            {
                return false;
            }

            var groundObjectCount = 0;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.gameObject.layer != groundLayer)
                {
                    continue;
                }

                groundObjectCount++;
                if (groundObjectCount >= 8)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureRuntimeLoadingCurtain()
        {
            if (runtimeLoadingCurtain != null)
            {
                return;
            }

            var curtain = new GameObject("Runtime Loading Curtain", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = curtain.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MinValue;

            var scaler = curtain.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundObject.transform.SetParent(curtain.transform, false);
            var backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            var background = backgroundObject.GetComponent<Image>();
            background.color = Color.black;
            background.raycastTarget = false;

            var sliderObject = new GameObject("Progress", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(curtain.transform, false);
            var sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = new Vector2(0f, -120f);
            sliderRect.sizeDelta = new Vector2(420f, 18f);

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(sliderObject.transform, false);
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillObject.GetComponent<Image>().color = Color.white;

            runtimeLoadingSlider = sliderObject.GetComponent<Slider>();
            runtimeLoadingSlider.minValue = 0f;
            runtimeLoadingSlider.maxValue = 1f;
            runtimeLoadingSlider.value = 0f;
            runtimeLoadingSlider.transition = Selectable.Transition.None;
            runtimeLoadingSlider.fillRect = fillRect;
            runtimeLoadingSlider.targetGraphic = fillObject.GetComponent<Image>();

            runtimeLoadingCurtain = curtain;
        }

        private void DestroyRuntimeLoadingCurtain()
        {
            if (runtimeLoadingCurtain == null)
            {
                return;
            }

            Destroy(runtimeLoadingCurtain);
            runtimeLoadingCurtain = null;
            runtimeLoadingSlider = null;
            runtimeLoadingCurtainGroup = null;
        }

        private void MovePlayerToStartPoint(Scene mapScene)
        {
            if (!movePlayerToMapStart || player == null || !mapScene.IsValid() || !mapScene.isLoaded)
            {
                return;
            }

            var startPoint = FindInScene(mapScene, "PlayerStartPoint");
            var startPosition = startPoint != null
                ? startPoint.position
                : Vector3.zero;
            Physics.SyncTransforms();
            startPosition = SnapToMapGround(startPosition);

            var startRotation = startPoint != null
                ? startPoint.rotation
                : Quaternion.identity;
            player.TeleportTo(startPosition, startRotation);
            player.SnapToGroundImmediate();
            cameraOrbit?.RecenterNow();
        }

        private void ConfigurePlayerStartExclusion(Scene mapScene)
        {
            if (enemySpawner == null || !mapScene.IsValid() || !mapScene.isLoaded)
            {
                return;
            }

            var startPoint = FindInScene(mapScene, "PlayerStartPoint");
            if (startPoint == null)
            {
                enemySpawner.ConfigurePlayerStartExclusion(Vector3.zero, false);
                return;
            }

            var startPosition = SnapToMapGround(startPoint.position);
            enemySpawner.ConfigurePlayerStartExclusion(startPosition, true);
        }

        private void ApplyMapBoundary(Scene mapScene, bool respawn = true)
        {
            if (enemySpawner == null || !mapScene.IsValid() || !mapScene.isLoaded)
            {
                return;
            }

            if (!TryGetBoundaryArea(mapScene, out var center, out var size))
            {
                return;
            }

            enemySpawner.ConfigureSpawnArea(center, size, respawn);
        }

        private bool TryGetBoundaryArea(Scene mapScene, out Vector3 center, out Vector2 size)
        {
            var hasBounds = false;
            var bounds = new Bounds();
            foreach (var boundaryRoot in FindAllInScene(mapScene, mapBoundaryRootName))
            {
                var colliders = boundaryRoot.GetComponentsInChildren<Collider>(true);
                foreach (var targetCollider in colliders)
                {
                    if (targetCollider == null || targetCollider.isTrigger || !targetCollider.enabled)
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        bounds = targetCollider.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(targetCollider.bounds);
                    }
                }
            }

            if (!hasBounds)
            {
                center = Vector3.zero;
                size = Vector2.zero;
                return false;
            }

            var inset = Mathf.Max(0f, mapBoundaryInset);
            center = new Vector3(bounds.center.x, 0f, bounds.center.z);
            size = new Vector2(
                Mathf.Max(1f, bounds.size.x - inset * 2f),
                Mathf.Max(1f, bounds.size.z - inset * 2f));
            return true;
        }

        private Vector3 SnapToMapGround(Vector3 position)
        {
            var rayStart = position + Vector3.up * groundProbeHeight;
            if (Physics.Raycast(rayStart, Vector3.down, out var hit, groundProbeDistance, mapGroundLayers, QueryTriggerInteraction.Ignore))
            {
                position = hit.point;
            }

            position.y += playerStartHeightOffset;
            return position;
        }

        private static Transform FindInScene(Scene scene, string targetName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var result = FindChildByName(root.transform, targetName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static System.Collections.Generic.List<Transform> FindAllInScene(Scene scene, string targetName)
        {
            var results = new System.Collections.Generic.List<Transform>();
            foreach (var root in scene.GetRootGameObjects())
            {
                FindChildrenByName(root.transform, targetName, results);
            }

            return results;
        }

        private static Transform FindChildByName(Transform root, string targetName)
        {
            if (root.name == targetName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var result = FindChildByName(root.GetChild(i), targetName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void FindChildrenByName(Transform root, string targetName, System.Collections.Generic.List<Transform> results)
        {
            if (IsMatchingSceneObjectName(root.name, targetName))
            {
                results.Add(root);
            }

            for (var i = 0; i < root.childCount; i++)
            {
                FindChildrenByName(root.GetChild(i), targetName, results);
            }
        }

        private static bool IsMatchingSceneObjectName(string objectName, string targetName)
        {
            return objectName == targetName
                || objectName.StartsWith(targetName + " (", System.StringComparison.Ordinal);
        }

        private string PickRandomMapScenePath()
        {
            if (mapScenePaths == null || mapScenePaths.Length == 0)
            {
                Debug.LogWarning("StageMapSceneLoader has no map scenes assigned.", this);
                return null;
            }

            if (mapScenePaths.Length == 1)
            {
                return mapScenePaths[0];
            }

            for (var attempt = 0; attempt < 8; attempt++)
            {
                var candidate = mapScenePaths[Random.Range(0, mapScenePaths.Length)];
                if (!avoidImmediateRepeat || candidate != loadedMapScenePath)
                {
                    return candidate;
                }
            }

            return mapScenePaths[Random.Range(0, mapScenePaths.Length)];
        }

        private string PickInitialRandomMapScenePath()
        {
            if (mapScenePaths == null || mapScenePaths.Length == 0)
            {
                Debug.LogWarning("StageMapSceneLoader has no map scenes assigned.", this);
                return null;
            }

            var initialCandidates = new List<string>();
            for (var i = 0; i < mapScenePaths.Length; i++)
            {
                var candidate = mapScenePaths[i];
                if (!IsMap4ScenePath(candidate))
                {
                    initialCandidates.Add(candidate);
                }
            }

            if (initialCandidates.Count == 0)
            {
                return PickRandomMapScenePath();
            }

            return initialCandidates[Random.Range(0, initialCandidates.Count)];
        }

        private static bool IsMap4ScenePath(string scenePath)
        {
            return !string.IsNullOrWhiteSpace(scenePath)
                && scenePath.EndsWith("map4.unity", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
