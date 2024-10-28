using Autofac;
using Autofac.Extras.Moq;
using Infrastructure.Enums;
using Infrastructure.NHibernateDatabase.AutofacModules;
using Infrastructure.NHibernateDatabase.Interfaces;
using Moq;
using Service.Test.Common.DatabaseSetup;
using Service.Test.Common.Extensions;
using Service.Test.Common.Modules;
using Service.Test.Common.TestDataGenerators;
using System.Reflection;

namespace Service.Test.Common;

public abstract class BaseTest
{
    protected ILifetimeScope Kernel { get; }

    public BaseTest()
    {
        // Initialize AutoFac container 
        Kernel = RootIoc.Container.BeginLifetimeScope(builder =>
        {
            ConfigureContainer(builder);

            // Register MassTransit testing Bus
            builder.Register(context => context.Resolve<TestBus>().LocalBus)
                   .As<MassTransit.IBus>()
                   .InstancePerLifetimeScope();

            builder.RegisterMock(new Mock<INHibernateDatabaseFactory>());
        });
    }

    /// <summary>
    /// AutoFac.Moq container
    /// </summary>
    public static AutoMock RootIoc = AutoMock.GetLoose(builder =>
    {
        // Register required database types to AutoFac
        NHibernateDbSetupTools.RegisterDatabase<TestDataSessionProvider>(builder, NHibernateDatabaseType.ExampleData);

        builder.RegisterType<TestDbBuilder>()
               .InstancePerLifetimeScope();

        builder.RegisterInstance(new TestDataSessionProviderCongfiguration
        {
            KeepDbAfterTest = false,
            DataSource = @"(localdb)\MSSQLLocalDB"
        });

        builder.RegisterModule(new TestLoggerModule());

        var generators = Assembly.GetExecutingAssembly()
                                     .GetTypes()
                                     .Where(type => !type.IsAbstract
                                            && type.Namespace != null
                                            && type.Namespace.EndsWith("Tests.Generators"))
                                     .ToArray();

        builder.RegisterTypes(generators)
               .AsClosedTypesOf(typeof(TDGenerator<>));
        builder.RegisterFactory<ITDGeneratorFactory>();
    });

    protected virtual void ConfigureContainer(ContainerBuilder builder) { }

    protected Task Responses<T>(Task<MassTransit.Fault<T>> fault, params Task[] successes)
    {
        var successful = Task.WhenAll(successes);
        return Task.WhenAny(successful, fault);
    }

    public void EnsureSuccessfulResponse<T>(Task<MassTransit.Fault<T>> faulted)
    {
        if (!faulted.IsCanceled)
            Assert.Fail($"Consumer failed to complete. { RenderException(faulted.Result.Exceptions) }");
    }

    string RenderException(MassTransit.ExceptionInfo[] exceptions)
    {
        if (exceptions[0] == null)
            return "";

        var message = exceptions[0].Message;
        var stackTrace = exceptions[0].StackTrace;
        var innerException = "";

        if (exceptions[0].InnerException != null)
            innerException = RenderException([exceptions[0].InnerException]);

        return $"\n{message}\n{innerException}\n{stackTrace}"; // TODO: Handle formatting if no inner exception
    }

    protected virtual ILifetimeScope GetIocContainer()
    {
        return Kernel.BeginLifetimeScope();
    }
}