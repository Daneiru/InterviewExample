using MassTransit;

namespace Kernel.MassTransit.Interfaces;

public interface IConsumerSubscription
{
    public static void Configure(IServiceBusBusFactoryConfigurator configurator, IBusRegistrationContext context,
        IServiceBusSubscription settings)
    { }
}