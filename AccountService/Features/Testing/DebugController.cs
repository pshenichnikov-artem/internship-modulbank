using AccountService.Common.Extensions;
using Messaging.Events;
using Messaging.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Testing;

/// <summary>
///     Отладочные эндпоинты для тестирования Outbox и RabbitMQ
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DebugController(IOutboxService outboxService) : ControllerBase
{
    [HttpPost("block-client/{clientId:guid}")]
    [SwaggerOperation(
        Summary = "Добавить событие ClientBlocked в Outbox",
        Description =
            "Тестирует отправку события блокировки клиента через Outbox в RabbitMQ. Событие попадет в очередь account.antifraud.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Событие ClientBlocked добавлено в Outbox для отправки в RabbitMQ")]
    public async Task<IActionResult> BlockClient(Guid clientId)
    {
        var clientBlocked = new ClientBlocked(clientId);
        await outboxService.AddAsync(clientBlocked, "client.blocked", CancellationToken.None);

        return Ok($"Отправлено событие блокировки клиента {clientId}");
    }

    [Authorize]
    [HttpGet("me")]
    [SwaggerOperation(
        Summary = "Получить OwnerId текущего пользователя",
        Description =
            "Возвращает ID текущего пользователя для использования в качестве OwnerId в других API запросах.")]
    [SwaggerResponse(StatusCodes.Status200OK, "OwnerId текущего пользователя", typeof(Guid))]
    public IActionResult GetMe()
    {
        return Ok(User.GetUserId());
    }

    [HttpPost("unblock-client/{clientId:guid}")]
    [SwaggerOperation(
        Summary = "Добавить событие ClientUnblocked в Outbox",
        Description =
            "Тестирует отправку события разблокировки клиента через Outbox в RabbitMQ. Событие попадет в очередь account.antifraud.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Событие ClientUnblocked добавлено в Outbox для отправки в RabbitMQ")]
    public async Task<IActionResult> UnblockClient(Guid clientId)
    {
        var clientUnblocked = new ClientUnblocked(clientId);
        await outboxService.AddAsync(clientUnblocked, "client.unblocked", CancellationToken.None);

        return Ok($"Отправлено событие разблокировки клиента {clientId}");
    }
}