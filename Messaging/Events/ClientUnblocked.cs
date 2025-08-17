using Messaging.Attribute;

namespace Messaging.Events;

[RoutingKey("client.unblocked")]
public sealed record ClientUnblocked(
    Guid ClientId) : IEvent;
