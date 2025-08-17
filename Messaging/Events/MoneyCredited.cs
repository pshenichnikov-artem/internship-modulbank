using Messaging.Attribute;

namespace Messaging.Events;

[RoutingKey("money.credited")]
public sealed record MoneyCredited(
    Guid AccountId,
    decimal Amount,
    string Currency,
    Guid OperationId) : IEvent;
