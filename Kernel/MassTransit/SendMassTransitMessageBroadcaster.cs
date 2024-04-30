using Kernel.MassTransit.Abstracts;
using Kernel.MassTransit.Interfaces;
using MassTransit;

namespace Kernel.MassTransit;

public class SendMassTransitMessageBroadcaster<TMessage> : BaseMassTransitMessageBroadcaster<TMessage>
    where TMessage : class
{
    protected IBroadcastEndpointDefinition<TMessage> EndpointDefinition { get; }
    protected ISendEndpointProvider EndpointProvider { get; }

    public SendMassTransitMessageBroadcaster(ISendEndpointProvider sendEndpointProvider, IBroadcastEndpointDefinition<TMessage> endpointDefinition)
    {
        EndpointProvider = sendEndpointProvider ?? throw new ArgumentNullException(nameof(sendEndpointProvider));
        EndpointDefinition = endpointDefinition ?? throw new ArgumentNullException(nameof(endpointDefinition));

        if (endpointDefinition.Uri == null)
            throw new ArgumentNullException(nameof(endpointDefinition), "Send Endpoint Uri was not setup correctly, and is null.");
    }

    protected override async Task OnPublishAsync(TMessage message, IDictionary<string, object>? headers, CancellationToken cancellationToken = default)
    {
        var endpoint = await EndpointProvider.GetSendEndpoint(EndpointDefinition.Uri);

        await endpoint.Send(message, sendContext => {
            PrepareHeaders(headers, (k, v) => sendContext.Headers.Set(k, v));
        }, cancellationToken);
    }
}