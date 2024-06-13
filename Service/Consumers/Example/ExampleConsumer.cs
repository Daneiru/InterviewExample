using AutoMapper;
using Domain.Event;
using Infrastructure.NHibernateDatabase.Interfaces;
using Kernel.MassTransit;
using MassTransit;
using Serilog;
using Service.Common;

namespace Service.Consumers.Example;

public class ExampleConsumer : BaseNHibernateConsumer<IExampleEvent>
{
    readonly PublishMassTransitMessageBroadcaster<IExamplePublish> PubMessageBroadcaster;
    readonly SendMassTransitMessageBroadcaster<IExampleSend> SendMessageBroadcaster;

    public ExampleConsumer(ILogger logger, INHibernateDatabaseFactory databaseFactory, IMapper mappingConfig,
                           PublishMassTransitMessageBroadcaster<IExamplePublish> pubMessageBroadcaster,
                           SendMassTransitMessageBroadcaster<IExampleSend> sendMessageBroadcaster) 
        : base(logger, databaseFactory, mappingConfig)
    {
        PubMessageBroadcaster = pubMessageBroadcaster;
        SendMessageBroadcaster = sendMessageBroadcaster;
    }

    public override async Task Consume(ConsumeContext<IExampleEvent> context)
    {
        using var database = DatabaseFactory.GetDbADataSession();
        database.Session.BeginTransaction();

        // Do data-y things...        
        // database.Session.GetAsync(id)
        // database.Session.UpdateAsync(obj)
        // etc
        var resultData = "Example data"; // Store data result here to send back in response

        database.Commit();


        var customHeaders = new Dictionary<string, object>()
        {
            // Additional message headers
        };

        // Publish completed message with custom headers
        await PubMessageBroadcaster.BroadcastAsync(new ExamplePublish(), customHeaders, context.CancellationToken);

        // Send complected message to explicit endpoint, with custom headers
        await SendMessageBroadcaster.BroadcastAsync(new ExampleSend(), customHeaders, context.CancellationToken);

        // Standard consumer response
        await context.RespondAsync<IExampleResponse>(new
        {
            Success = true,
            Data = resultData,
        });
    }
}
