using Kernel.MassTransit.Interfaces;
using MassTransit;
using MassTransit.Configuration;

namespace Kernel.MassTransit.Abstracts;

public abstract class BaseConsumerSubscription<TConsumer>
    where TConsumer : class, IConsumer
{
    /// <summary>
    /// Establishes a Mass Transit consumer with an Azure Service Bus topic and subscription.
    /// If either the Topic or Subscription in the provided settings are empty, this configuration is skipped.
    /// </summary>
    /// <param name="serviceBusFactoryConfigurator">Service Bus configurator</param>
    /// <param name="context">Mass Transit context</param>
    /// <param name="settings">Subscription settings</param>
    public static void ConfigureSubscription(IServiceBusBusFactoryConfigurator serviceBusFactoryConfigurator, 
        IBusRegistrationContext context, IServiceBusSubscription? settings)
    {
        // If no subscription or topic provided, do not register subscription
        if (settings == null 
            || string.IsNullOrWhiteSpace(settings.Subscription) 
            || string.IsNullOrWhiteSpace(settings.Topic)) 
            return;

        serviceBusFactoryConfigurator.SubscriptionEndpoint(
            settings.Subscription,
            settings.Topic,
            endpointConfigurator =>
            {
                endpointConfigurator.ConfigureDeadLetterQueueDeadLetterTransport();
                endpointConfigurator.ConfigureDeadLetterQueueErrorTransport();
                endpointConfigurator.ConfigureConsumer<TConsumer>(context);
                endpointConfigurator.UseJsonSerializer();
            });
    }

    /// <summary>
    /// Registers the consumer with Mass Transit.
    /// If either the Topic or Subscription in the provided settings are empty, the consumer will NOT be registered.
    /// </summary>
    /// <param name="massTransitConfigurator">Mass Transit configurator</param>
    /// <param name="settings">Subscription settings</param>
    public static void RegisterConsumer(IBusRegistrationConfigurator massTransitConfigurator, 
        IServiceBusSubscription? settings)
    {
        // If no subscription or topic provided, do not register consumer
        if (settings == null 
            || string.IsNullOrWhiteSpace(settings.Subscription) 
            || string.IsNullOrWhiteSpace(settings.Topic)) 
            return;

        massTransitConfigurator.RegisterConsumer<TConsumer>();
    }
}