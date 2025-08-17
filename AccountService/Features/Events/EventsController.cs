using Messaging.Events;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Events;

/// <summary>
///     Документация событий RabbitMQ с примерами структуры сообщений(контроллер не отправляет события в rabbitmq, а служит
///     для документации)
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}")]
[SwaggerTag("Примеры событий RabbitMQ (только для документации)")]
public class EventsController : ControllerBase
{
    /// <summary>
    ///     Пример события AccountOpened, отправляемого в Outbox
    /// </summary>
    /// <remarks>
    ///     Отправляется при создании нового счета.
    ///     **Источник:** `POST /api/v1/accounts` (CreateAccountHandler)
    ///     **Routing Key:** `account.opened`
    ///     **Очереди:** account.crm, account.audit
    /// </remarks>
    [HttpPost("account-opened")]
    [SwaggerOperation(
        Summary = "Событие открытия счета",
        Description = "Отправляется при создании нового счета"
    )]
    [SwaggerResponse(200, "Пример события", typeof(MessageEnvelopeDto<AccountOpened>))]
    public ActionResult<MessageEnvelopeDto<AccountOpened>> AccountOpened()
    {
        var payload = new AccountOpened(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
            "USD",
            "Checking"
        );

        return Ok(new MessageEnvelopeDto<AccountOpened>(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new MessageMetaDto("v1", "account-service", Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
            payload
        ));
    }

    /// <summary>
    ///     Пример события AccountClosed, отправляемого в Outbox
    /// </summary>
    /// <remarks>
    ///     Отправляется при удалении/закрытии счета.
    ///     **Источник:** `DELETE /api/v1/accounts/{id}` (DeleteAccountHandler)
    ///     **Routing Key:** `account.closed`
    ///     **Очереди:** account.crm, account.audit
    /// </remarks>
    [HttpPost("account-closed")]
    [SwaggerOperation(
        Summary = "Событие закрытия счета",
        Description = "Отправляется при удалении/закрытии счета"
    )]
    [SwaggerResponse(200, "Пример события", typeof(MessageEnvelopeDto<AccountClosed>))]
    public ActionResult<MessageEnvelopeDto<AccountClosed>> AccountClosed()
    {
        var payload = new AccountClosed(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
            "USD"
        );

        return Ok(new MessageEnvelopeDto<AccountClosed>(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new MessageMetaDto("v1", "account-service", Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
            payload
        ));
    }

    /// <summary>
    ///     Пример события AccountUpdated, отправляемого в Outbox
    /// </summary>
    /// <remarks>
    ///     Отправляется при изменении параметров счета (валюта, процентная ставка).
    ///     **Источник:** `PUT /api/v1/accounts/{id}`, `PATCH /api/v1/accounts/{id}/{fieldName}` (UpdateAccountHandler,
    ///     UpdateAccountFieldHandler)
    ///     **Routing Key:** `account.modified`
    ///     **Очереди:** account.crm, account.audit
    /// </remarks>
    [HttpPost("account-updated")]
    [SwaggerOperation(
        Summary = "Событие обновления счета",
        Description = "Отправляется при изменении параметров счета"
    )]
    [SwaggerResponse(200, "Пример события", typeof(MessageEnvelopeDto<AccountUpdated>))]
    public ActionResult<MessageEnvelopeDto<AccountUpdated>> AccountUpdated()
    {
        var payload = new AccountUpdated(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
            "EUR",
            2.5m
        );

        return Ok(new MessageEnvelopeDto<AccountUpdated>(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new MessageMetaDto("v1", "account-service", Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
            payload
        ));
    }

    /// <summary>
    ///     Событие пополнения счета
    /// </summary>
    /// <remarks>
    ///     Отправляется при зачислении средств на счет.
    ///     **Источник:** `POST /api/v1/transactions` (CreateTransactionHandler - Credit)
    ///     **Routing Key:** `money.credited`
    ///     **Очереди:** account.notifications, account.audit
    /// </remarks>
    [HttpPost("money-credited")]
    [SwaggerOperation(
        Summary = "Событие пополнения счета",
        Description = "Отправляется при зачислении средств на счет"
    )]
    [SwaggerResponse(200, "Пример события", typeof(MessageEnvelopeDto<MoneyCredited>))]
    public ActionResult<MessageEnvelopeDto<MoneyCredited>> MoneyCredited()
    {
        var payload = new MoneyCredited(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            1000.50m,
            "USD",
            Guid.Parse("550e8400-e29b-41d4-a716-446655440002")
        );

        return Ok(new MessageEnvelopeDto<MoneyCredited>(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new MessageMetaDto("v1", "account-service", Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
            payload
        ));
    }

    /// <summary>
    ///     Событие списания со счета
    /// </summary>
    /// <remarks>
    ///     Отправляется при списании средств со счета.
    ///     **Источник:** `POST /api/v1/transactions` (CreateTransactionHandler - Debit)
    ///     **Routing Key:** `money.debited`
    ///     **Очереди:** account.notifications, account.audit
    /// </remarks>
    [HttpPost("money-debited")]
    [SwaggerOperation(
        Summary = "Событие списания со счета",
        Description = "Отправляется при списании средств со счета"
    )]
    [SwaggerResponse(200, "Пример события", typeof(MessageEnvelopeDto<MoneyDebited>))]
    public ActionResult<MessageEnvelopeDto<MoneyDebited>> MoneyDebited()
    {
        var payload = new MoneyDebited(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            500.25m,
            "USD",
            Guid.Parse("550e8400-e29b-41d4-a716-446655440002")
        );

        return Ok(new MessageEnvelopeDto<MoneyDebited>(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new MessageMetaDto("v1", "account-service", Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
            payload
        ));
    }

    /// <summary>
    ///     Событие перевода между счетами
    /// </summary>
    /// <remarks>
    ///     Отправляется при выполнении перевода между счетами.
    ///     **Источник:** `POST /api/v1/transactions` (CreateTransactionHandler - Transfer)
    ///     **Routing Key:** `money.transfer`
    ///     **Очереди:** account.notifications, account.audit
    /// </remarks>
    [HttpPost("money-transfer")]
    [SwaggerOperation(
        Summary = "Событие перевода между счетами",
        Description = "Отправляется при выполнении перевода между счетами"
    )]
    [SwaggerResponse(200, "Пример события", typeof(MessageEnvelopeDto<MoneyTransfer>))]
    public ActionResult<MessageEnvelopeDto<MoneyTransfer>> MoneyTransfer()
    {
        var payload = new MoneyTransfer(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
            750.00m,
            "USD",
            Guid.Parse("550e8400-e29b-41d4-a716-446655440002")
        );

        return Ok(new MessageEnvelopeDto<MoneyTransfer>(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new MessageMetaDto("v1", "account-service", Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
            payload
        ));
    }

    /// <summary>
    ///     Событие блокировки клиента
    /// </summary>
    /// <remarks>
    ///     Отправляется при блокировке клиента.
    ///     **Routing Key:** `client.blocked`
    ///     **Очереди:** account.antifraud, account.audit
    /// </remarks>
    [HttpPost("client-blocked")]
    [SwaggerOperation(
        Summary = "Событие блокировки клиента",
        Description = "Отправляется при блокировке клиента"
    )]
    [SwaggerResponse(200, "Пример события", typeof(MessageEnvelopeDto<ClientBlocked>))]
    public ActionResult<MessageEnvelopeDto<ClientBlocked>> ClientBlocked()
    {
        var payload = new ClientBlocked(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440001")
        );

        return Ok(new MessageEnvelopeDto<ClientBlocked>(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new MessageMetaDto("v1", "account-service", Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
            payload
        ));
    }

    /// <summary>
    ///     Событие разблокировки клиента
    /// </summary>
    /// <remarks>
    ///     Отправляется при разблокировке клиента.
    ///     **Routing Key:** `client.unblocked`
    ///     **Очереди:** account.antifraud, account.audit
    /// </remarks>
    [HttpPost("client-unblocked")]
    [SwaggerOperation(
        Summary = "Событие разблокировки клиента",
        Description = "Отправляется при разблокировке клиента"
    )]
    [SwaggerResponse(200, "Пример события", typeof(MessageEnvelopeDto<ClientUnblocked>))]
    public ActionResult<MessageEnvelopeDto<ClientUnblocked>> ClientUnblocked()
    {
        var payload = new ClientUnblocked(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440001")
        );

        return Ok(new MessageEnvelopeDto<ClientUnblocked>(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new MessageMetaDto("v1", "account-service", Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
            payload
        ));
    }

    /// <summary>
    ///     Событие начисления процентов
    /// </summary>
    /// <remarks>
    ///     Отправляется при начислении процентов по депозитному счету.
    ///     **Источник:** Hangfire Job (AccrueInterestHandler)
    ///     **Routing Key:** `interest.accrued`
    ///     **Очереди:** account.audit
    /// </remarks>
    [HttpPost("interest-accrued")]
    [SwaggerOperation(
        Summary = "Событие начисления процентов",
        Description = "Отправляется при начислении процентов по депозитному счету"
    )]
    [SwaggerResponse(200, "Пример события", typeof(MessageEnvelopeDto<InterestAccrued>))]
    public ActionResult<MessageEnvelopeDto<InterestAccrued>> InterestAccrued()
    {
        var payload = new InterestAccrued(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.Today),
            25.75m
        );

        return Ok(new MessageEnvelopeDto<InterestAccrued>(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new MessageMetaDto("v1", "account-service", Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
            payload
        ));
    }

    /// <summary>
    ///     Событие отмены транзакции
    /// </summary>
    /// <remarks>
    ///     Отправляется при отмене транзакции.
    ///     **Источник:** `POST /api/v1/transactions/{id}/cancel` (CancelTransactionAsync)
    ///     **Routing Key:** `transaction.canceled`
    ///     **Очереди:** account.transactions, account.audit
    /// </remarks>
    [HttpPost("transaction-canceled")]
    [SwaggerOperation(
        Summary = "Событие отмены транзакции",
        Description = "Отправляется при отмене транзакции"
    )]
    [SwaggerResponse(200, "Пример события", typeof(MessageEnvelopeDto<TransactionCanceled>))]
    public ActionResult<MessageEnvelopeDto<TransactionCanceled>> TransactionCanceled()
    {
        var payload = new TransactionCanceled(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
            Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
            500.00m,
            "USD",
            "Transfer"
        );

        return Ok(new MessageEnvelopeDto<TransactionCanceled>(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new MessageMetaDto("v1", "account-service", Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
            payload
        ));
    }

    /// <summary>
    ///     Событие обновления транзакции
    /// </summary>
    /// <remarks>
    ///     Отправляется при изменении описания транзакции.
    ///     **Источник:** `PUT /api/v1/transactions/{id}` (UpdateTransactionHandler)
    ///     **Routing Key:** `transaction.updated`
    ///     **Очереди:** account.transactions, account.audit
    /// </remarks>
    [HttpPost("transaction-updated")]
    [SwaggerOperation(
        Summary = "Событие обновления транзакции",
        Description = "Отправляется при изменении описания транзакции"
    )]
    [SwaggerResponse(200, "Пример события", typeof(MessageEnvelopeDto<TransactionUpdated>))]
    public ActionResult<MessageEnvelopeDto<TransactionUpdated>> TransactionUpdated()
    {
        var payload = new TransactionUpdated(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
            Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            "Updated payment description"
        );

        return Ok(new MessageEnvelopeDto<TransactionUpdated>(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new MessageMetaDto("v1", "account-service", Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
            payload
        ));
    }

    /// <summary>
    ///     Обертка для событий в RabbitMQ
    /// </summary>
    /// <typeparam name="T">Тип события в payload</typeparam>
    public sealed record MessageEnvelopeDto<T>(
        Guid EventId,
        DateTime OccurredAt,
        MessageMetaDto Meta,
        T Payload
    );

    /// <summary>
    ///     Метаданные сообщения
    /// </summary>
    public sealed record MessageMetaDto(
        string Version,
        string Source,
        string CorrelationId,
        string CausationId
    );
}