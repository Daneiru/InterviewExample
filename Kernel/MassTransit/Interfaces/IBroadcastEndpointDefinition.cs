namespace Kernel.MassTransit.Interfaces;

public interface IBroadcastEndpointDefinition<TMessage>
    where TMessage : class
{
    public Uri Uri { get; }
}