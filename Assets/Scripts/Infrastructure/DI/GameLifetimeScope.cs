using VContainer;
using VContainer.Unity;
using DinoGrow.Core.Combat;
using DinoGrow.Core.Data;
using DinoGrow.Core.Enemy;
using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;
using DinoGrow.Camera;
using DinoGrow.Gameplay.Enemy;
using DinoGrow.Gameplay.Player;
using DinoGrow.Gameplay.Stage;
using DinoGrow.Infrastructure.Data;
using DinoGrow.Infrastructure.DI;
using DinoGrow.Infrastructure.Events;
using DinoGrow.Infrastructure.Pooling;
using DinoGrow.UI;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-10000)]
public class GameLifetimeScope : LifetimeScope
{
    [Header("Scene Components")]
    [SerializeField] private PlayerDinoController player;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private StageMapSceneLoader stageMapSceneLoader;
    [SerializeField] private GameIntroSequence gameIntroSequence;
    [SerializeField] private GameHud gameHud;
    [SerializeField] private GameHudHeartUI heartUI;
    [SerializeField] private Transform gameplayCamera;
    [SerializeField] private GameObject loadingOverlayPanel;
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private CinemachineThirdPersonOrbit cameraOrbit;

    [Header("Effects")]
    [SerializeField] private ParticleSystem bloodEffectPrefab;
    [SerializeField] private AudioClip eatingSoundClip;
    [SerializeField, Range(0f, 1f)] private float eatingSoundVolume = 1f;
    [SerializeField] private AudioClip stageClearSoundClip;
    [SerializeField] private AudioSource stageClearSoundSource;
    [SerializeField, Range(0f, 1f)] private float stageClearSoundVolume = 1f;
    [SerializeField] private AudioClip backgroundMusicClip;
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.7f;

    [Header("Generated Data")]
    [SerializeField] private DinoDatabase dinoDatabase;
    [SerializeField] private StageDatabase stageDatabase;
    [SerializeField] private SpawnDatabase spawnDatabase;
    [SerializeField] private PlayerGrowthDatabase playerGrowthDatabase;

    protected override void Awake()
    {
        ShowInitialLoadingOverlay();
        base.Awake();
    }

    private void OnEnable()
    {
        ShowInitialLoadingOverlay();
    }

    protected override void Configure(IContainerBuilder builder)
    {
        var dinoRepository = new DinoDataRepository(dinoDatabase);
        var stageRepository = new StageDataRepository(stageDatabase);
        var spawnRepository = new SpawnDataRepository(spawnDatabase);
        var playerGrowthRepository = new PlayerGrowthDataRepository(playerGrowthDatabase);

        builder.Register<GameEventBus>(Lifetime.Singleton);
        builder.RegisterInstance(new CameraReference(gameplayCamera));
        builder.Register<EatResolver>(Lifetime.Singleton);
        builder.Register<EnemyBehaviorResolver>(Lifetime.Singleton);
        builder.Register<GrowthSystem>(Lifetime.Singleton);
        builder.RegisterInstance(CreatePlayerProgress(dinoRepository, playerGrowthRepository));
        builder.Register<GameStateController>(Lifetime.Singleton);
        builder.Register<StageRule>(Lifetime.Singleton);
        builder.Register<IObjectPoolService, ObjectPoolService>(Lifetime.Singleton);
        builder.RegisterInstance(new DeathEffectSettings(bloodEffectPrefab));
        builder.Register<DeathEffectService>(Lifetime.Singleton);
        builder.RegisterInstance(new EatingSoundSettings(eatingSoundClip, eatingSoundVolume));
        builder.Register<EatingSoundService>(Lifetime.Singleton);
        builder.RegisterInstance(dinoRepository);
        builder.RegisterInstance(stageRepository);
        builder.RegisterInstance(spawnRepository);
        builder.RegisterInstance(playerGrowthRepository);

        if (player != null)
        {
            if (heartUI == null && gameHud != null)
            {
                heartUI = gameHud.GetComponentInChildren<GameHudHeartUI>(true);
            }

            player.ConfigureHeartUI(heartUI);
            builder.RegisterComponent(player);
        }

        if (gameHud != null)
        {
            builder.RegisterComponent(gameHud);
        }

        if (enemySpawner != null)
        {
            builder.RegisterComponent(enemySpawner);
        }

        if (stageMapSceneLoader == null)
        {
            stageMapSceneLoader = GetComponent<StageMapSceneLoader>();
            if (stageMapSceneLoader == null)
            {
                stageMapSceneLoader = gameObject.AddComponent<StageMapSceneLoader>();
            }
        }

        builder.RegisterComponent(stageMapSceneLoader);
        stageMapSceneLoader.ConfigureLoadingOverlay(loadingOverlayPanel, loadingSlider);
        stageMapSceneLoader.ConfigureHudVisibilityTargets(gameHud, heartUI);
        stageMapSceneLoader.ConfigureCameraOrbit(cameraOrbit);
        stageMapSceneLoader.ConfigureEnemySpawner(enemySpawner);
        stageMapSceneLoader.ConfigureStageClearSound(stageClearSoundClip, stageClearSoundSource, stageClearSoundVolume);
        stageMapSceneLoader.ConfigureBackgroundMusic(backgroundMusicClip, backgroundMusicSource, backgroundMusicVolume);

        if (gameIntroSequence == null && player != null)
        {
            gameIntroSequence = player.GetComponent<GameIntroSequence>();
        }

        if (gameIntroSequence != null)
        {
            gameIntroSequence.ConfigurePlayerCameraOrbit(cameraOrbit);
            stageMapSceneLoader.ConfigureStartOverlaySequence(gameIntroSequence);
            builder.RegisterComponent(gameIntroSequence);
        }
    }

    private void ShowInitialLoadingOverlay()
    {
        if (loadingOverlayPanel == null)
        {
            return;
        }

        loadingOverlayPanel.SetActive(true);
        loadingOverlayPanel.transform.SetAsLastSibling();

        var canvas = loadingOverlayPanel.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = loadingOverlayPanel.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = short.MaxValue;

        var canvasGroup = loadingOverlayPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = loadingOverlayPanel.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;

        if (loadingSlider != null)
        {
            loadingSlider.value = 0f;
        }
    }

    private static PlayerProgress CreatePlayerProgress(
        DinoDataRepository dinoRepository,
        PlayerGrowthDataRepository playerGrowthRepository)
    {
        var startLevel = PlayerProgress.DefaultStartLevel;
        var startExp = 0;
        var maxLevel = playerGrowthRepository.MaxLevel > 0
            ? playerGrowthRepository.MaxLevel
            : PlayerProgress.DefaultMaxLevel;
        var expToLevelUp = PlayerProgress.DefaultExpToLevelUp;

        if (dinoRepository.TryGetById("player", out var playerData))
        {
            startLevel = playerData.level;
            startExp = playerData.exp;
        }

        if (playerGrowthRepository.TryGetByLevel(startLevel, out var growthData) && growthData.requiredExp > 0)
        {
            expToLevelUp = growthData.requiredExp;
        }

        return new PlayerProgress(
            startLevel,
            startExp,
            maxLevel,
            expToLevelUp,
            playerGrowthRepository.CreateRequiredExpMap());
    }
}
