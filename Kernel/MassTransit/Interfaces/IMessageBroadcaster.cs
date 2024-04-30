namespace Kernel.MassTransit.Interfaces;

public interface IMessageBroadcaster<in TMessage>
    where TMessage : class
{
    Task BroadcastAsync(TMessage message, CancellationToken cancellationToken);
    Task BroadcastAsync(TMessage message, IDictionary<string, object> headers, CancellationToken cancellationToken);
}

public interface IMessageBroadcaster<in TMessage, TResponse>
    where TMessage : class
{
    Task<TResponse> BroadcastAsync(TMessage message, CancellationToken cancellationToken);
}