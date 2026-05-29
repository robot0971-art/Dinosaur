using System.Collections;
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

public sealed class DeathEffectSettings
{
    public DeathEffectSettings(ParticleSystem bloodEffectPrefab)
    {
        BloodEffectPrefab = bloodEffectPrefab;
    }

    public ParticleSystem BloodEffectPrefab { get; }
}

public sealed class EatingSoundSettings
{
    public EatingSoundSettings(AudioClip clip, float volume)
    {
        Clip = clip;
        Volume = Mathf.Clamp01(volume);
    }

    public AudioClip Clip { get; }
    public float Volume { get; }
}

public sealed class EatingSoundService
{
    private const float SpatialBlend = 0.85f;
    private const float MinDistance = 1.5f;
    private const float MaxDistance = 22f;

    private readonly EatingSoundSettings settings;

    public EatingSoundService(EatingSoundSettings settings)
    {
        this.settings = settings;
    }

    public void PlayAt(Vector3 position)
    {
        if (settings.Clip == null || settings.Volume <= 0f)
        {
            return;
        }

        var soundObject = new GameObject("EatingSound");
        soundObject.transform.position = position;

        var source = soundObject.AddComponent<AudioSource>();
        source.clip = settings.Clip;
        source.volume = settings.Volume;
        source.spatialBlend = SpatialBlend;
        source.minDistance = MinDistance;
        source.maxDistance = MaxDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.Play();

        Object.Destroy(soundObject, settings.Clip.length + 0.1f);
    }
}

public sealed class DeathEffectService
{
    private readonly DeathEffectSettings settings;
    private readonly IObjectPoolService poolService;
    private Material fallbackBloodMaterial;
    private Texture2D fallbackBloodTexture;

    public DeathEffectService(DeathEffectSettings settings, IObjectPoolService poolService)
    {
        this.settings = settings;
        this.poolService = poolService;
    }

    public void SpawnBlood(Vector3 position)
    {
        if (settings.BloodEffectPrefab != null)
        {
            SpawnPrefabBlood(position);
            return;
        }

        SpawnRuntimeBlood(position);
    }

    private void SpawnPrefabBlood(Vector3 position)
    {
        var effect = poolService.Spawn(settings.BloodEffectPrefab, position, Quaternion.identity);
        if (effect == null)
        {
            return;
        }

        var returner = effect.GetComponent<PooledParticleReturner>();
        if (returner == null)
        {
            returner = effect.gameObject.AddComponent<PooledParticleReturner>();
        }

        returner.Play(effect, poolService);
    }

    private void SpawnRuntimeBlood(Vector3 position)
    {
        var effectObject = new GameObject("RuntimeBloodEffect");
        effectObject.transform.position = position;

        var effect = effectObject.AddComponent<ParticleSystem>();
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ConfigureRuntimeBloodParticle(effect);

        var renderer = effect.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = GetFallbackBloodMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 1f;

        var returner = effect.GetComponent<PooledParticleReturner>();
        if (returner == null)
        {
            returner = effectObject.AddComponent<PooledParticleReturner>();
        }

        returner.Play(effect, poolService);
    }

    private static void ConfigureRuntimeBloodParticle(ParticleSystem effect)
    {
        var main = effect.main;
        main.duration = 0.45f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 5.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.45f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.02f, 0.01f, 0.95f),
            new Color(0.35f, 0.0f, 0.0f, 0.75f));
        main.gravityModifier = 0.9f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = effect.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 22, 34)
        });

        var shape = effect.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.22f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var colorOverLifetime = effect.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.85f, 0.02f, 0.01f), 0f),
                new GradientColorKey(new Color(0.35f, 0f, 0f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;
    }

    private Material GetFallbackBloodMaterial()
    {
        if (fallbackBloodMaterial != null)
        {
            return fallbackBloodMaterial;
        }

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        fallbackBloodMaterial = new Material(shader)
        {
            name = "Runtime Blood Particle Material"
        };

        if (fallbackBloodMaterial.HasProperty("_BaseColor"))
        {
            fallbackBloodMaterial.SetColor("_BaseColor", new Color(0.85f, 0.05f, 0.03f, 0.85f));
        }
        else if (fallbackBloodMaterial.HasProperty("_Color"))
        {
            fallbackBloodMaterial.SetColor("_Color", new Color(0.85f, 0.05f, 0.03f, 0.85f));
        }

        if (fallbackBloodMaterial.HasProperty("_BaseMap"))
        {
            fallbackBloodMaterial.SetTexture("_BaseMap", GetFallbackBloodTexture());
        }
        else if (fallbackBloodMaterial.HasProperty("_MainTex"))
        {
            fallbackBloodMaterial.SetTexture("_MainTex", GetFallbackBloodTexture());
        }

        ConfigureFallbackBloodMaterialTransparency(fallbackBloodMaterial);

        return fallbackBloodMaterial;
    }

    private static void ConfigureFallbackBloodMaterialTransparency(Material material)
    {
        SetFloatIfPresent(material, "_Surface", 1f);
        SetFloatIfPresent(material, "_Blend", 0f);
        SetFloatIfPresent(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        SetFloatIfPresent(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        SetFloatIfPresent(material, "_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        SetFloatIfPresent(material, "_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        SetFloatIfPresent(material, "_ZWrite", 0f);
        SetFloatIfPresent(material, "_AlphaClip", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private Texture2D GetFallbackBloodTexture()
    {
        if (fallbackBloodTexture != null)
        {
            return fallbackBloodTexture;
        }

        const int size = 64;
        fallbackBloodTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime Blood Circle Texture",
            wrapMode = TextureWrapMode.Clamp
        };

        var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        var radius = size * 0.46f;
        var feather = size * 0.08f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), center);
                var alpha = Mathf.Clamp01((radius - distance) / feather);
                fallbackBloodTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        fallbackBloodTexture.Apply();
        return fallbackBloodTexture;
    }
}

public sealed class PooledParticleReturner : MonoBehaviour
{
    private Coroutine returnRoutine;

    public void Play(ParticleSystem rootParticle, IObjectPoolService poolService)
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
        }

        var particles = rootParticle.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var particle in particles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }

        returnRoutine = StartCoroutine(ReturnAfterDelay(rootParticle, particles, poolService));
    }

    private IEnumerator ReturnAfterDelay(
        ParticleSystem rootParticle,
        ParticleSystem[] particles,
        IObjectPoolService poolService)
    {
        var delay = 0f;
        foreach (var particle in particles)
        {
            var main = particle.main;
            delay = Mathf.Max(delay, main.duration + main.startLifetime.constantMax);
        }

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        returnRoutine = null;
        poolService.Despawn(rootParticle);
    }
}
