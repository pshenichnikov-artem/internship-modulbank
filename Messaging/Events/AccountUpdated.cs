using Messaging.Attribute;

namespace Messaging.Events;

[RoutingKey("account.updated")]
public sealed record AccountUpdated(
    Guid AccountId,
    Guid OwnerId,
    string Currency,
    decimal? InterestRate) : IEvent;