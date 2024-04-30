namespace Kernel.MassTransit.Interfaces;

public interface IServiceBusSubscription
{
    string? Topic { get; }
    string? Subscription { get; }
}