using Messaging.Attribute;

namespace Messaging.Events;

[RoutingKey("client.blocked")]
public sealed record ClientBlocked(
    Guid ClientId) : IEvent;
