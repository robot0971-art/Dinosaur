using System.Collections;
using VContainer;
using VContainer.Unity;
using DinoGrow.Core.Combat;
using DinoGrow.Core.Data;
using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;
using DinoGrow.Gameplay.Enemy;
using DinoGrow.Gameplay.Player;
using DinoGrow.Infrastructure.Data;
using DinoGrow.Infrastructure.Events;
using DinoGrow.Infrastructure.Pooling;
using DinoGrow.UI;
using UnityEngine;

public class GameLifetimeScope : LifetimeScope
{
    [Header("Scene Components")]
    [SerializeField] private PlayerDinoController player;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private GameHud gameHud;

    [Header("Effects")]
    [SerializeField] private ParticleSystem bloodEffectPrefab;

    [Header("Generated Data")]
    [SerializeField] private DinoDatabase dinoDatabase;
    [SerializeField] private StageDatabase stageDatabase;
    [SerializeField] private SpawnDatabase spawnDatabase;
    [SerializeField] private PlayerGrowthDatabase playerGrowthDatabase;

    protected override void Configure(IContainerBuilder builder)
    {
        var dinoRepository = new DinoDataRepository(dinoDatabase);
        var stageRepository = new StageDataRepository(stageDatabase);
        var spawnRepository = new SpawnDataRepository(spawnDatabase);
        var playerGrowthRepository = new PlayerGrowthDataRepository(playerGrowthDatabase);

        builder.Register<GameEventBus>(Lifetime.Singleton);
        builder.Register<EatResolver>(Lifetime.Singleton);
        builder.Register<GrowthSystem>(Lifetime.Singleton);
        builder.RegisterInstance(CreatePlayerProgress(dinoRepository, playerGrowthRepository));
        builder.Register<GameStateController>(Lifetime.Singleton);
        builder.Register<StageRule>(Lifetime.Singleton);
        builder.Register<IObjectPoolService, ObjectPoolService>(Lifetime.Singleton);
        builder.RegisterInstance(new DeathEffectSettings(bloodEffectPrefab));
        builder.Register<DeathEffectService>(Lifetime.Singleton);
        builder.RegisterInstance(dinoRepository);
        builder.RegisterInstance(stageRepository);
        builder.RegisterInstance(spawnRepository);
        builder.RegisterInstance(playerGrowthRepository);

        if (player != null)
        {
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

        return new PlayerProgress(startLevel, startExp, maxLevel, expToLevelUp);
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

public sealed class DeathEffectService
{
    private readonly DeathEffectSettings settings;
    private readonly IObjectPoolService poolService;

    public DeathEffectService(DeathEffectSettings settings, IObjectPoolService poolService)
    {
        this.settings = settings;
        this.poolService = poolService;
    }

    public void SpawnBlood(Vector3 position)
    {
        if (settings.BloodEffectPrefab == null)
        {
            return;
        }

        var effect = poolService.Spawn(settings.BloodEffectPrefab, position, Quaternion.identity);
        var returner = effect.GetComponent<PooledParticleReturner>();
        if (returner == null)
        {
            returner = effect.gameObject.AddComponent<PooledParticleReturner>();
        }

        returner.Play(effect, poolService);
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
