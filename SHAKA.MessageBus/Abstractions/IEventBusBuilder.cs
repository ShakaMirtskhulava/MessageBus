using Microsoft.Extensions.DependencyInjection;

namespace MessageBus.Abstractions;

public interface IEventBusBuilder
{
    public IServiceCollection Services { get; }
}
