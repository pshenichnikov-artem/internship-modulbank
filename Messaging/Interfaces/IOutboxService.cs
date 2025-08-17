using Messaging.Events;

namespace Messaging.Interfaces;

public interface IOutboxService
{
    Task AddAsync<T>(T payload, CancellationToken ct = default) where T : IEvent;
    Task AddAsync<T>(T payload, string routingKey, CancellationToken ct = default) where T : IEvent;
    Task PublishAsync(Guid eventId, CancellationToken ct = default);
    Task<List<Guid>> GetUnpublishedAsync(CancellationToken ct = default);
}