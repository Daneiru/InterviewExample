using Kernel.Extensions;
using MassTransit;
using Serilog;

namespace Kernel.MassTransit.Abstracts;

public abstract class BaseConsumer<T> : IConsumer<T>, IConsumer<Fault<T>>
    where T : class
{
    protected readonly ILogger Logger;

    protected BaseConsumer(ILogger logger)
    {
        Logger = logger;
    }

    public abstract Task Consume(ConsumeContext<T> context);

    public virtual Task Consume(ConsumeContext<Fault<T>> context)
    {
        Logger.Error(context.Message.ToExceptionString());
        return Task.FromResult(0);
    }
}
