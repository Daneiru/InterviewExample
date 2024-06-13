using MassTransit.Testing;
using MassTransit;

namespace Service.Test.Common;

public class TestBus : IAsyncDisposable, IDisposable
{
    public TestBus(Action<IInMemoryBusFactoryConfigurator> busConfigurator, Action<IInMemoryReceiveEndpointConfigurator> endpointConfigurator)
    {
        BusConfigurator = busConfigurator;
        EndpointConfigurator = endpointConfigurator;
    }

    InMemoryTestHarness Harness { get; } = new InMemoryTestHarness();
    public IBus? LocalBus { get; private set; }

    Action<IInMemoryBusFactoryConfigurator> BusConfigurator { get; }
    Action<IInMemoryReceiveEndpointConfigurator> EndpointConfigurator { get; }

    /// <summary>
    /// Retrieves a TimeSpan that represents a window of time. The size of the window is determined by the mode the code is in:
    /// Debugging or not. By default, the window is 5 seconds normally and 60 seconds while debugging to give time to debug. This
    /// can be adjusted per test by setting the Timeout and DebuggingTimeout properties respectively.
    /// </summary>
    public TimeSpan TimeoutWindow
    {
        get
        {
            return TimeSpan.FromSeconds(System.Diagnostics.Debugger.IsAttached ? DebuggingTimeout : Timeout);
        }
    }

    /// <summary>
    /// Gets or sets the length in seconds of the TimeoutWindow while Debugging.
    /// </summary>
    public int DebuggingTimeout { get; set; } = 60;

    /// <summary>
    /// Gets or sets the length in seconds of the TimeoutWindow at runtime
    /// </summary>
    public int Timeout { get; set; } = 10;

    CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

    List<Action<IInMemoryReceiveEndpointConfigurator>> Checkers = new List<Action<IInMemoryReceiveEndpointConfigurator>>();
    List<Tuple<ManualResetEvent, Action<ManualResetEvent, IInMemoryReceiveEndpointConfigurator>>> Actions = new List<Tuple<ManualResetEvent, Action<ManualResetEvent, IInMemoryReceiveEndpointConfigurator>>>();

    private void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
    {
        foreach (var checks in Checkers)
            checks.Invoke(configurator);

        foreach (var action in Actions)
            action.Item2.Invoke(action.Item1, configurator);

        EndpointConfigurator?.Invoke(configurator);
    }

    protected void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
    {
        BusConfigurator?.Invoke(configurator);
    }

    public async Task Start()
    {
        Harness.OnConfigureInMemoryBus += ConfigureInMemoryBus;
        Harness.OnConfigureInMemoryReceiveEndpoint += ConfigureInMemoryReceiveEndpoint;
        await Harness.Start();
        Harness.TestTimeout = TimeoutWindow;
        LocalBus = Harness.Bus;
    }

    public Task<T> CheckFor<T>() where T : class
    {
        return CheckFor<T>(TimeoutWindow);
    }

    public Task<T> CheckFor<T>(TimeSpan timeout) where T : class
    {
        Task<ConsumeContext<T>> hold = null;

        var result = new Task<T>(() =>
        {
            var token = CancellationTokenSource.Token;
            hold.Wait((int)timeout.TotalMilliseconds, token);   //Exception here is benign, continure debugging
            return hold.Result.Message;
        }, CancellationTokenSource.Token);

        Checkers.Add(config =>
        {
            hold = Harness.Handled<T>(config);
            result.Start();
        });

        return result;
    }

    public Task Responses<T>(Task<Fault<T>> fault, params Task[] successes)
    {
        var successful = Task.WhenAll(successes)
            .ContinueWith(t => { }, CancellationTokenSource.Token);  // ensures successfull is cancelable

        successful.ConfigureAwait(false);

        Task.WhenAny(fault, successful)
            .ContinueWith(t => {
                CancellationTokenSource.Cancel(true);
            }).ConfigureAwait(false);

        return Task.WhenAll(successful, fault)
                   .ContinueWith(t => t.IsCanceled ? Task.CompletedTask : t);
    }

    string RenderException(ExceptionInfo[] exceptions)
    {
        if (exceptions[0] == null)
            return "";

        var message = exceptions[0].Message;
        var stackTrace = exceptions[0].StackTrace;
        var innerException = RenderException([exceptions[0].InnerException]);

        return $"\n{message}\n{innerException}\n{stackTrace}";
    }

    void DefaultHandler<T>() where T : class { }

    public ManualResetEvent AddHandler<T>(Action<T> action) where T : class
    {
        var result = new ManualResetEvent(false);

        Actions.Add(new Tuple<ManualResetEvent, Action<ManualResetEvent, IInMemoryReceiveEndpointConfigurator>>(result, (mre, configurator) =>
            configurator.Handler<T>(handler => Task.Run(() => action.Invoke(handler.Message))
                        .ContinueWith(c => mre.Set())
                        .ContinueWith(c => handler.Message))));

        return result;
    }

    public ManualResetEvent AddHandler<T>() where T : class
    {
        var result = new ManualResetEvent(false);

        Actions.Add(new Tuple<ManualResetEvent, Action<ManualResetEvent, IInMemoryReceiveEndpointConfigurator>>(result, (mre, configurator) =>
            configurator.Handler<T>(handler => Task.Run(DefaultHandler<T>)
                        .ContinueWith(c => mre.Set())
                        .ContinueWith(c => handler.Message))));

        return result;
    }

    public bool MessageConsumed<T>() where T : class
    {
        return Harness.Consumed.Select<T>().Any();
    }

    public IEnumerable<T> PublishedMessages<T>() where T : class
    {
        return Harness.Published.Select<T>().Select(t => t.Context.Message);
    }

    public IEnumerable<T> SentMessages<T>() where T : class
    {
        return Harness.Sent.Select<T>().Select(t => t.Context.Message);
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Harness.Stop().Wait();
                Harness.OnConfigureInMemoryBus -= ConfigureInMemoryBus;
                Harness.OnConfigureInMemoryReceiveEndpoint -= ConfigureInMemoryReceiveEndpoint;
                Harness.Dispose();
            }

            disposedValue = true;
        }
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
    }

    public async ValueTask DisposeAsync()
    {
        await Harness.Stop();
        Harness.OnConfigureInMemoryBus -= ConfigureInMemoryBus;
        Harness.OnConfigureInMemoryReceiveEndpoint -= ConfigureInMemoryReceiveEndpoint;
        Harness.Dispose();
    }
    #endregion
}
