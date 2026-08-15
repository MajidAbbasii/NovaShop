using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;

namespace NovaShop.Api.Extensions;

public class MassTransitPublishEndpointStub : IPublishEndpoint
{
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => new NoOpConnectHandle();

    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
        => Task.CompletedTask;

    public Task Publish<T>(T message, IPipe<PublishContext<T>> pipe, CancellationToken cancellationToken = default) where T : class
        => Task.CompletedTask;

    public Task Publish<T>(T message, IPipe<PublishContext> pipe, CancellationToken cancellationToken = default) where T : class
        => Task.CompletedTask;

    public Task Publish(object message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task Publish(object message, IPipe<PublishContext> pipe, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task Publish(object message, Type messageType, IPipe<PublishContext> pipe, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class
        => Task.CompletedTask;

    public Task Publish<T>(object values, IPipe<PublishContext<T>> pipe, CancellationToken cancellationToken = default) where T : class
        => Task.CompletedTask;

    public Task Publish<T>(object values, IPipe<PublishContext> pipe, CancellationToken cancellationToken = default) where T : class
        => Task.CompletedTask;

    private class NoOpConnectHandle : ConnectHandle
    {
        public void Disconnect() { }
        public void Dispose() { }
    }
}
