using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Sample.Api.Consumers;
using Sample.Contracts;

namespace Sample.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private ITestHarness _testHarness;

    public CustomWebApplicationFactory()
    {

    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<NotifyCustomerOrderSubmittedConsumer>();
            });
        });
    }

    public ITestHarness InitializeMassTransitTestHarness()
    {
        if (this._testHarness == null)
        {
            this._testHarness = this.Server.Services.GetService<ITestHarness>();
            this._testHarness.TestInactivityTimeout = TimeSpan.FromSeconds(1);
            this._testHarness.Start();
        }
        return this._testHarness;
    }
}

public class NotifyCustomerOrderSubmittedConsumerTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public NotifyCustomerOrderSubmittedConsumerTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Consume_OrderSubmitted()
    {
        //Arrange 
        var harness = _factory.InitializeMassTransitTestHarness();

        var @event = new OrderSubmitted(Guid.NewGuid());

        //Act
        await harness.Bus.Publish(@event);
        await harness.InactivityTask;

        //Assert
        var published = await harness.Published.Any<OrderSubmitted>();
        var consumed = await harness.Consumed.Any<OrderSubmitted>();
        var faults = await harness.Published.Any<Fault<OrderSubmitted>>();

        Assert.True(published);
        Assert.True(consumed);
        Assert.False(faults);
    }

}