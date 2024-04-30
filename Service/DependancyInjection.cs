using Domain.Event;
using Kernel.MassTransit;
using Kernel.MassTransit.Constants;
using Kernel.MassTransit.Interfaces;
using Kernel.MassTransit.Models;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Service.Consumers.Example;

namespace Service;

public static class DependancyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var exampleSubscriptionSettings = 
            configuration.GetValue<ExampleSubscriptionSettings>($"{ConfigurationConstants.SubscriptionSettingsSection}:{nameof(ExampleSubscriptionSettings)}");

        services.AddMassTransit(mt =>
        {
            mt.UsingAzureServiceBus((busContext, config) =>
            {
                ExampleConsumerSubscription.ConfigureSubscription(config, busContext, exampleSubscriptionSettings);
            });

            ExampleConsumerSubscription.RegisterConsumer(mt, exampleSubscriptionSettings);
        });

        services.AddSingleton<PublishMassTransitMessageBroadcaster<IExamplePublish>>();

        RegisterSendEndpoints(services, configuration);

        return services;
    }

    private static void RegisterSendEndpoints(IServiceCollection services, IConfiguration configuration)
    {
        var exampleSendEndpoint = configuration.GetValue<Uri>($"{ConfigurationConstants.SendEndpointsSection}:ExampleSendEndpoint");
        
        if ( exampleSendEndpoint != null ) {
            services.AddSingleton<IBroadcastEndpointDefinition<IExampleSend>>(new BroadcastEndpointDefinition<IExampleSend>(exampleSendEndpoint));
            services.AddSingleton<SendMassTransitMessageBroadcaster<IExampleSend>>();
        }        
    }
}
