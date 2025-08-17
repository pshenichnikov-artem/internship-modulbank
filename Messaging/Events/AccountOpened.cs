using Messaging.Attribute;

namespace Messaging.Events;

[RoutingKey("account.opened")]
public sealed record AccountOpened(
    Guid AccountId,
    Guid OwnerId,
    string Currency,
    string Type) : IEvent;
