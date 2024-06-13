using Autofac;
using Moq;

namespace Service.Test.Common.Extensions;

public static class LifetimeScopeTestingExtensions
{
    public static TestBus GetTestBus(this ILifetimeScope lifetimeScope, Action<MassTransit.IInMemoryBusFactoryConfigurator>? busConfigurator = null, Action<MassTransit.IInMemoryReceiveEndpointConfigurator>? endpointConfigurator = null)
    {
        return lifetimeScope.Resolve<TestBus>(new NamedParameter("busConfigurator", busConfigurator), new NamedParameter("endpointConfigurator", endpointConfigurator));
    }

    public static Mock<T> GetMock<T>(this ILifetimeScope lifetimeScope) where T : class
    {
        return Mock.Get(lifetimeScope.Resolve<T>());
    }
}
