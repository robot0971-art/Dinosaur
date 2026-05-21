using System.Collections;
using DinoGrow.Core.Growth;
using DinoGrow.Camera;
using DinoGrow.Gameplay.Enemy;
using DinoGrow.Gameplay.Player;
using DinoGrow.Infrastructure.Events;
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
            "Assets/Scenes/map7.unity"
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
        [SerializeField] private CinemachineThirdPersonOrbit cameraOrbit;
        [SerializeField] private EnemySpawner enemySpawner;

        private GameEventBus eventBus;
        private PlayerDinoController player;
        private string loadedMapScenePath;
        private bool isSwitching;

        private void Awake()
        {
            UseGroundLayerIfAvailable();
            SetLoadingProgress(0f, false);
        }

        [Inject]
        public void Construct(GameEventBus eventBus, PlayerDinoController player)
        {
            this.eventBus = eventBus;
            this.player = player;
        }

        public void ConfigureLoadingOverlay(GameObject panel, Slider slider)
        {
            loadingOverlayPanel = panel;
            loadingSlider = slider;
            SetLoadingProgress(0f, false);
        }

        public void ConfigureCameraOrbit(CinemachineThirdPersonOrbit orbit)
        {
            cameraOrbit = orbit;
        }

        public void ConfigureEnemySpawner(EnemySpawner spawner)
        {
            enemySpawner = spawner;
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
                DisableExistingSceneMapRoots();
                StartCoroutine(LoadInitialRandomMapRoutine());
            }
        }

        private void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.PlayerGrowthChanged -= OnPlayerGrowthChanged;
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

            StartCoroutine(SwitchMapRoutine(nextScenePath));
        }

        private IEnumerator LoadInitialRandomMapRoutine()
        {
            var nextScenePath = PickRandomMapScenePath();
            if (string.IsNullOrWhiteSpace(nextScenePath))
            {
                yield break;
            }

            SetLoadingProgress(0f, true);
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
            ApplyMapBoundary(nextScene);
            MovePlayerToStartPoint(nextScene);
            loadedMapScenePath = nextScenePath;
            yield return null;
            SetLoadingProgress(1f, true);
            yield return null;
            SetLoadingProgress(0f, false);
        }

        private IEnumerator SwitchMapRoutine(string nextScenePath)
        {
            isSwitching = true;
            SetLoadingProgress(0f, true);
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
            ApplyMapBoundary(nextScene);
            MovePlayerToStartPoint(nextScene);
            loadedMapScenePath = nextScenePath;
            yield return null;
            SetLoadingProgress(1f, true);
            yield return null;
            SetLoadingProgress(0f, false);
            isSwitching = false;
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
            if (loadingOverlayPanel != null)
            {
                loadingOverlayPanel.SetActive(visible);
            }

            if (loadingSlider != null)
            {
                loadingSlider.value = Mathf.Clamp01(progress);
            }
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

                if (ShouldKeepMainSceneRoot(root))
                {
                    continue;
                }

                if (LooksLikeMapRoot(root))
                {
                    root.SetActive(false);
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

            if (root.name.Contains("Map") || root.name.Contains("Ground") || root.name.Contains("Environment"))
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

        private void ApplyMapBoundary(Scene mapScene)
        {
            if (enemySpawner == null || !mapScene.IsValid() || !mapScene.isLoaded)
            {
                return;
            }

            var boundaryRoot = FindInScene(mapScene, mapBoundaryRootName);
            if (boundaryRoot == null || !TryGetBoundaryArea(boundaryRoot, out var center, out var size))
            {
                return;
            }

            enemySpawner.ConfigureSpawnArea(center, size, true);
        }

        private bool TryGetBoundaryArea(Transform boundaryRoot, out Vector3 center, out Vector2 size)
        {
            var colliders = boundaryRoot.GetComponentsInChildren<Collider>(true);
            var hasBounds = false;
            var bounds = new Bounds();
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
    }
}
