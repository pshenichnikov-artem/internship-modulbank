using AccountService.Common.Models.Api;
using AccountService.Features.Transactions.Commands.CancelTransaction;
using AccountService.Features.Transactions.Commands.CreateTransaction;
using AccountService.Features.Transactions.Commands.UpdateTransaction;
using AccountService.Features.Transactions.Models;
using AccountService.Features.Transactions.Query.GetTransactionById;
using AccountService.Features.Transactions.Query.GetTransactions;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Transactions;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TransactionsController(IMediator mediator) : ControllerBase
{
    [HttpPost("search")]
    [SwaggerOperation(Summary = "Получить список транзакций",
        Description = "Позволяет фильтровать и сортировать транзакции по счетам, датам, типам и т.д.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Список транзакций", typeof(IEnumerable<TransactionDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка запроса", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
    public async Task<IActionResult> GetTransactions(
        [FromBody] GetTransactionsQuery query)
    {
        var result = await mediator.Send(query);
        return ApiResult.FromCommandResult(result);
    }

    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Получить транзакцию по ID")]
    [SwaggerResponse(StatusCodes.Status200OK, "Транзакция найдена", typeof(TransactionDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Транзакция не найдена", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Неверный ID", typeof(ErrorResponse))]
    public async Task<IActionResult> GetTransaction(
        [FromRoute] Guid id,
        [FromQuery] List<string>? fields = null)
    {
        var query = new GetTransactionByIdQuery(id, fields);
        var result = await mediator.Send(query);
        return ApiResult.FromCommandResult(result);
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Создать новую транзакцию")]
    [SwaggerResponse(StatusCodes.Status201Created, "Транзакция создана", typeof(TransactionDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Счет не найден", typeof(ErrorResponse))]
    public async Task<IActionResult> CreateTransaction(
        [FromBody] CreateTransactionCommand request)
    {
        var result = await mediator.Send(request);
        return ApiResult.FromCommandResult(result, nameof(GetTransaction), new { id = result.Data });
    }

    [HttpPut("{id}")]
    [SwaggerOperation(Summary = "Обновить описание существующей транзакции")]
    [SwaggerResponse(StatusCodes.Status200OK, "Обновление выполнено", typeof(TransactionDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Транзакция не найдена", typeof(ErrorResponse))]
    public async Task<IActionResult> UpdateTransaction(
        [FromRoute] Guid id,
        [FromBody] string description)
    {
        var command = new UpdateTransactionCommand
        {
            Id = id,
            Description = description
        };
        var result = await mediator.Send(command);
        return ApiResult.FromCommandResult(result);
    }

    [HttpPost("{id}/cancel")]
    [SwaggerOperation(Summary = "Отменить транзакцию",
        Description =
            "Отменяет транзакцию и корректирует баланс счета. Для Checking и Deposit счетов баланс не может стать отрицательным.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Транзакция отменена")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации или недостаточно средств",
        typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Транзакция не найдена", typeof(ErrorResponse))]
    public async Task<IActionResult> CancelTransaction([FromRoute] Guid id)
    {
        var command = new CancelTransactionCommand
        {
            TransactionId = id
        };

        var result = await mediator.Send(command);
        return ApiResult.FromCommandResult(result);
    }
}