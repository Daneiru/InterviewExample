namespace Kernel.MassTransit.Interfaces;

public interface IRetrySettings
{
    int RetryLimit { get; }
    double MinIntervalInSeconds { get; }
    double MaxIntervalInSeconds { get; }
    double IntervalDeltaInSeconds { get; }
}