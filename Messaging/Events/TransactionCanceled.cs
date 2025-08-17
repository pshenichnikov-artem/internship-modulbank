using Messaging.Attribute;

namespace Messaging.Events;

[RoutingKey("transaction.canceled")]
public sealed record TransactionCanceled(
    Guid TransactionId,
    Guid AccountId,
    Guid? CounterpartyAccountId,
    decimal Amount,
    string Currency,
    string TransactionType) : IEvent;