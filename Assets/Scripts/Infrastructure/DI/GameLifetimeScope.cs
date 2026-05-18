using VContainer;
using VContainer.Unity;
using DinoGrow.Core.Combat;
using DinoGrow.Core.Data;
using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;
using DinoGrow.Gameplay.Player;
using DinoGrow.Infrastructure.Data;
using DinoGrow.Infrastructure.Events;
using DinoGrow.UI;
using UnityEngine;

public class GameLifetimeScope : LifetimeScope
{
    [Header("Scene Components")]
    [SerializeField] private PlayerDinoController player;
    [SerializeField] private GameHud gameHud;

    [Header("Generated Data")]
    [SerializeField] private DinoDatabase dinoDatabase;
    [SerializeField] private StageDatabase stageDatabase;
    [SerializeField] private SpawnDatabase spawnDatabase;
    [SerializeField] private PlayerGrowthDatabase playerGrowthDatabase;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<GameEventBus>(Lifetime.Singleton);
        builder.Register<EatResolver>(Lifetime.Singleton);
        builder.Register<GrowthSystem>(Lifetime.Singleton);
        builder.RegisterInstance(new PlayerProgress());
        builder.Register<GameStateController>(Lifetime.Singleton);
        builder.Register<StageRule>(Lifetime.Singleton);
        builder.RegisterInstance(new DinoDataRepository(dinoDatabase));
        builder.RegisterInstance(new StageDataRepository(stageDatabase));
        builder.RegisterInstance(new SpawnDataRepository(spawnDatabase));
        builder.RegisterInstance(new PlayerGrowthDataRepository(playerGrowthDatabase));

        if (player != null)
        {
            builder.RegisterComponent(player);
        }

        if (gameHud != null)
        {
            builder.RegisterComponent(gameHud);
        }
    }
}
