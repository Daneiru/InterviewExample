using Autofac;
using Autofac.Extras.Moq;
using ImpromptuInterface;
using Moq;
using Service.Test.Common.DatabaseSetup.Factory;

namespace Service.Test.Common.Extensions;

public static class ContainerBuilderExtensions
{
    public static void RegisterFactory<TFactory>(this ContainerBuilder builder) where TFactory : class
    {
        builder.RegisterGeneric(typeof(TestFactoryProvider<,>));
        builder.Register(provider => provider.Resolve<TestFactoryProvider<TFactory, DefaultTestFactoryResolver>>().ActLike<TFactory>()).As<TFactory>();
    }

    public static void RegisterFactory<TFactory, TResolver>(this ContainerBuilder builder) where TFactory : class where TResolver : ITestFactoryResolver
    {
        builder.RegisterGeneric(typeof(TestFactoryProvider<,>));
        builder.RegisterType<TResolver>();
        builder.Register(provider => provider.Resolve<TestFactoryProvider<TFactory, TResolver>>().ActLike<TFactory>()).As<TFactory>();
    }

    public static void RegisterMockOf<T>(this ContainerBuilder builder) where T : class
    {
        builder.RegisterMock(new Mock<T>());
    }
}
