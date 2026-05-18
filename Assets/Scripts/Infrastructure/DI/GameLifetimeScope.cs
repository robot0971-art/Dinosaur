using DinoGrow.Core.Combat;
using DinoGrow.Core.Growth;
using DinoGrow.Core.Stage;
using DinoGrow.Infrastructure.Events;
using VContainer;
using VContainer.Unity;

namespace Dino.Infrastructure.DI
{
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            // Core Services
            builder.Register<GameEventBus>(Lifetime.Singleton);
            builder.Register<EatResolver>(Lifetime.Singleton);
            builder.Register<GrowthSystem>(Lifetime.Singleton);
            builder.Register<PlayerProgress>(Lifetime.Singleton);
            builder.Register<StageRule>(Lifetime.Singleton);
            builder.Register<GameStateController>(Lifetime.Singleton);

            // Feature 2 test subscriber. If the object does not exist, VContainer just ignores it.
            builder.RegisterComponentInHierarchy<EventBusSubscriberExample>();
        }
    }
}
