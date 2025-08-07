using System.Text.Json;
using AccountService.Common.Constants;
using AccountService.Common.Extensions;
using AccountService.Common.Models.Api;
using AccountService.Features.Accounts.Commands.CreateAccount;
using AccountService.Features.Accounts.Commands.DeleteAccount;
using AccountService.Features.Accounts.Commands.UpdateAccount;
using AccountService.Features.Accounts.Commands.UpdateAccountField;
using AccountService.Features.Accounts.Model;
using AccountService.Features.Accounts.Query.GetAccountById;
using AccountService.Features.Accounts.Query.GetAccounts;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AccountsController(IMediator mediator) : ControllerBase
{
    [HttpPost("search")]
    [SwaggerOperation(Summary = "Получить список счетов",
        Description = "Позволяет фильтровать и сортировать счета по владельцам, валютам, типам и статусу")]
    [SwaggerResponse(StatusCodes.Status200OK, "Список счетов", typeof(SuccessResponse<IEnumerable<AccountDto>>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, SwaggerMessages.ValidationError, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, SwaggerMessages.Unauthorized, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, SwaggerMessages.InternalError, typeof(ErrorResponse))]
    public async Task<IActionResult> PostSearchAccounts(
        GetAccountsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return ApiResult.FromCommandResult(result);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Получить счет по ID")]
    [SwaggerResponse(StatusCodes.Status200OK, "Счет найден", typeof(SuccessResponse<AccountDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, SwaggerMessages.ValidationError, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, SwaggerMessages.Unauthorized, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, SwaggerMessages.NotFound, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, SwaggerMessages.InternalError, typeof(ErrorResponse))]
    public async Task<IActionResult> GetAccount(
        [FromRoute] Guid id,
        [FromQuery] List<string>? fields = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAccountByIdQuery(id, fields);
        var result = await mediator.Send(query, cancellationToken);
        return ApiResult.FromCommandResult(result);
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Создать новый счет")]
    [SwaggerResponse(StatusCodes.Status201Created, "Счет создан", typeof(SuccessResponse<Guid>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, SwaggerMessages.ValidationError, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, SwaggerMessages.Unauthorized, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, SwaggerMessages.NotFound, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, SwaggerMessages.InternalError, typeof(ErrorResponse))]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateAccountCommand
        {
            OwnerId = User.GetUserId(),
            Currency = request.Currency,
            Type = request.Type,
            InterestRate = request.InterestRate
        };
        var result = await mediator.Send(command, cancellationToken);
        return ApiResult.FromCommandResult(result, nameof(GetAccount),
            new { id = result.IsSuccess ? result.Data : Guid.Empty });
    }

    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Обновить существующий счет")]
    [SwaggerResponse(StatusCodes.Status200OK, "Обновление выполнено", typeof(SuccessResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, SwaggerMessages.ValidationError, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, SwaggerMessages.Unauthorized, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, SwaggerMessages.Forbidden, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, SwaggerMessages.NotFound, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, SwaggerMessages.InternalError, typeof(ErrorResponse))]
    public async Task<IActionResult> UpdateAccount(
        [FromRoute] Guid id,
        [FromBody] UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateAccountCommand
        {
            Id = id,
            Currency = request.Currency,
            InterestRate = request.InterestRate,
            OwnerId = User.GetUserId()
        };
        var result = await mediator.Send(command, cancellationToken);
        return ApiResult.FromCommandResult(result);
    }

    [HttpPatch("{id:guid}/{fieldName}")]
    [SwaggerOperation(
        Summary = "Обновить отдельное поле счета",
        Description =
            "Допустимые поля: 'Currency' — трехбуквенный код валюты, 'InterestRate' — процентная ставка"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Поле обновлено", typeof(SuccessResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, SwaggerMessages.ValidationError, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, SwaggerMessages.Unauthorized, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, SwaggerMessages.Forbidden, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, SwaggerMessages.NotFound, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, SwaggerMessages.InternalError, typeof(ErrorResponse))]
    public async Task<IActionResult> UpdateAccountField(
        [FromRoute] Guid id,
        [FromRoute] string fieldName,
        [FromBody] JsonElement fieldValue,
        CancellationToken cancellationToken)
    {
        object? typedValue = fieldName.ToLower() switch
        {
            "currency" => fieldValue.ValueKind == JsonValueKind.String
                ? fieldValue.GetString()?.ToUpperInvariant()
                : ApiResult.BadRequest($"Неверный формат значения для поля {fieldName}"),

            // ReSharper disable once StringLiteralTypo Слово приведено к нижнему регистру
            "interestrate" => fieldValue.ValueKind == JsonValueKind.Number && fieldValue.TryGetDecimal(out var rate)
                ? rate
                : ApiResult.BadRequest($"Неверный формат значения для поля {fieldName}"),

            _ => ApiResult.BadRequest($"Поле {fieldName} не существует")
        };

        if (typedValue is ObjectResult badResult) return badResult;

        var command = new UpdateAccountFieldCommand
        {
            Id = id,
            FieldName = fieldName,
            FieldValue = typedValue!,
            OwnerId = User.GetUserId()
        };

        var result = await mediator.Send(command, cancellationToken);
        return ApiResult.FromCommandResult(result);
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Удалить счет(закрыть)",
        Description = "Удаляет счет, если его баланс равен нулю. Для всех типов счетов баланс должен быть равен нулю.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Счет успешно удален", typeof(SuccessResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, SwaggerMessages.ValidationError, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, SwaggerMessages.Unauthorized, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, SwaggerMessages.Forbidden, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, SwaggerMessages.NotFound, typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, SwaggerMessages.InternalError, typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAccount(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteAccountCommand
        {
            AccountId = id,
            OwnerId = User.GetUserId()
        };

        var result = await mediator.Send(command, cancellationToken);
        return ApiResult.FromCommandResult(result);
    }
}