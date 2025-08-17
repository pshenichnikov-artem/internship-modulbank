using Messaging.Attribute;

namespace Messaging.Events;

[RoutingKey("money.transfer")]
public sealed record MoneyTransfer(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency,
    Guid TransferId) : IEvent;