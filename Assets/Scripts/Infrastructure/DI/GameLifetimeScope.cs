using VContainer;
using VContainer.Unity;
using DinoGrow.Core.Combat;
using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;
using DinoGrow.Infrastructure.Events;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<GameEventBus>(Lifetime.Singleton);
        builder.Register<EatResolver>(Lifetime.Singleton);
        builder.Register<GrowthSystem>(Lifetime.Singleton);
        builder.Register<PlayerProgress>(Lifetime.Singleton);
        builder.Register<GameStateController>(Lifetime.Singleton);
        builder.Register<StageRule>(Lifetime.Singleton);
    }
}
