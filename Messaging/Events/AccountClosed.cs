using Messaging.Attribute;

namespace Messaging.Events;

[RoutingKey("account.closed")]
public sealed record AccountClosed(
    Guid AccountId,
    Guid OwnerId,
    string Currency) : IEvent;
