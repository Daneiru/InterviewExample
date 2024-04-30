using Kernel.MassTransit.Interfaces;

namespace Kernel.MassTransit.Models;

public record StandardRetrySettings : IRetrySettings
{
    public int RetryLimit { get; init; } = 5;
    public double MinIntervalInSeconds { get; init; } = 10;
    public double MaxIntervalInSeconds { get; init; } = 90;
    public double IntervalDeltaInSeconds { get; init; } = 2;
}