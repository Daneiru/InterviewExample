using Kernel.MassTransit.Interfaces;

namespace Kernel.MassTransit.Models;

public record BroadcastEndpointDefinition<TMessage>(Uri Uri) : IBroadcastEndpointDefinition<TMessage>
    where TMessage : class;