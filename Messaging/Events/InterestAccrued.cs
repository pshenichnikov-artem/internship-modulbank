using Messaging.Attribute;

namespace Messaging.Events;

[RoutingKey("interest.accrued")]
public sealed record InterestAccrued(
    Guid AccountId,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    decimal Amount) : IEvent;
