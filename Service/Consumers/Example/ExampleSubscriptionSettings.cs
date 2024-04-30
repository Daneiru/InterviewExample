using Kernel.MassTransit.Interfaces;

namespace Service.Consumers.Example;

public class ExampleSubscriptionSettings : IServiceBusSubscription
{
    public string? Topic { get; set; }
    public string? Subscription { get; set; }
}
