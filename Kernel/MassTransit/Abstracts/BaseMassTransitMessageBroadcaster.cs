using Kernel.MassTransit.Interfaces;

namespace Kernel.MassTransit.Abstracts;

public abstract class BaseMassTransitMessageBroadcaster<TMessage> : IMessageBroadcaster<TMessage>
    where TMessage : class
{

    public Task BroadcastAsync(TMessage message, CancellationToken cancellationToken = default)
        => OnPublishAsync(message, null, cancellationToken);
    public Task BroadcastAsync(TMessage message, IDictionary<string, object> headers, CancellationToken cancellationToken = default)
        => OnPublishAsync(message, headers, cancellationToken);

    protected abstract Task OnPublishAsync(TMessage message, IDictionary<string, object>? headers, CancellationToken cancellationToken = default);

    protected void PrepareHeaders(IDictionary<string, object>? headers, Action<string, object> apply)
    {
        if (headers == null) return;

        foreach (var pair in headers)
            apply(pair.Key, pair.Value);
    }
}