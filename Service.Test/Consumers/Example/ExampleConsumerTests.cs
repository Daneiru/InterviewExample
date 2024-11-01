﻿using Autofac;
using Domain.Event;
using Infrastructure.Entities;
using Infrastructure.Enums;
using MassTransit;
using Service.Consumers.Example;
using Service.Test.Common;
using Service.Test.Common.DatabaseSetup;
using Service.Test.Common.Extensions;

namespace Service.Test.Consumers.Example;

public class ExampleConsumerTests : BaseTest
{
    protected void OnConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
    {
        configurator.Consumer<ExampleConsumer>(Kernel.Resolve<IRegistrationContext>());
        configurator.UseInMemoryOutbox();
    }

    [Test]
    public async Task Test1()
    {
        await using var ioc = GetIocContainer();
        var database = ioc.SetupDbMock(NHibernateDatabaseType.ExampleData);
        using var dbBuilder = ioc.GetDatabaseBuilder(NHibernateDatabaseType.ExampleData);

        dbBuilder.Create<ExampleEntity>(a => {
            a.Id = 123;
            a.Name = "Test name";
        });
        dbBuilder.Create<ExampleEntity>(a => a.Id = 1);
        dbBuilder.Create<ExampleEntity>(a => a.Id = 5);
        dbBuilder.Generate();

        await using var bus = ioc.GetTestBus(endpointConfigurator: OnConfigureInMemoryReceiveEndpoint);
        var checkForPublish = bus.CheckFor<IExampleResponse>();

        await bus.Start();
        await bus.LocalBus.Publish<IExampleEvent>(new { });
        var fault = bus.CheckFor<Fault<IExampleEvent>>();
        await bus.Responses(fault, checkForPublish);

        var message = checkForPublish.Result;
        Assert.IsNotNull(message);
    }
}
