using Kernel.MassTransit.Abstracts;
using MassTransit;

namespace Kernel.MassTransit;

public class PublishMassTransitMessageBroadcaster<TMessage> : BaseMassTransitMessageBroadcaster<TMessage>
    where TMessage : class
{
    protected IPublishEndpoint Endpoint { get; }

    public PublishMassTransitMessageBroadcaster(IPublishEndpoint publishEndpoint)
    {
        Endpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    }

    protected override async Task OnPublishAsync(TMessage message, IDictionary<string, object>? headers, CancellationToken cancellationToken = default)
    {
        await Endpoint.Publish(message, publishContext => {
            PrepareHeaders(headers, (k, v) => publishContext.Headers.Set(k, v));
        }, cancellationToken);
    }
}