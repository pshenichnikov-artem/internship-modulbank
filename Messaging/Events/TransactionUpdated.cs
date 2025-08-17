using Messaging.Attribute;

namespace Messaging.Events;

[RoutingKey("transaction.updated")]
public sealed record TransactionUpdated(
    Guid TransactionId,
    Guid AccountId,
    string? Description) : IEvent;