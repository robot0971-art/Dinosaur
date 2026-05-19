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
using DinoGrow.UI;
using UnityEngine;

public class GameLifetimeScope : LifetimeScope
{
    [Header("Scene Components")]
    [SerializeField] private PlayerDinoController player;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private GameHud gameHud;

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
