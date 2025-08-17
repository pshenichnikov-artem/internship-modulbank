using Messaging.Attribute;

namespace Messaging.Events;

[RoutingKey("money.debited")]
public sealed record MoneyDebited(
    Guid AccountId,
    decimal Amount,
    string Currency,
    Guid OperationId) : IEvent;
