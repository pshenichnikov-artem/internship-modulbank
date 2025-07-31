using System.Text.Json;
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
    private static Guid GetCurrentUserId()
    {
        return Guid.Parse("8d2f5a44-77d7-4e91-9b52-353f7fbeef04");
    }

    [HttpPost("search")]
    [SwaggerOperation(Summary = "Получить список счетов",
        Description = "Позволяет фильтровать и сортировать счета по владельцам, валютам, типам и статусу")]
    [SwaggerResponse(StatusCodes.Status200OK, "Список счетов", typeof(IEnumerable<AccountDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка запроса", typeof(ErrorResponse))]
    public async Task<IActionResult> GetAccounts(
        GetAccountsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return ApiResult.FromCommandResult(result);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Получить счет по ID")]
    [SwaggerResponse(StatusCodes.Status200OK, "Счет найден", typeof(AccountDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Счет не найден")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации")]
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
    [SwaggerResponse(StatusCodes.Status201Created, "Счет создан", typeof(AccountDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Клиент не найден")]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        request.OwnerId = GetCurrentUserId();
        var result = await mediator.Send(request, cancellationToken);
        return ApiResult.FromCommandResult(result, nameof(GetAccount),
            new { id = result.IsSuccess ? result.Data : Guid.Empty });
    }

    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Обновить существующий счет")]
    [SwaggerResponse(StatusCodes.Status200OK, "Обновление выполнено", typeof(AccountDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Недостаточно прав")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Счет не найден")]
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
            OwnerId = GetCurrentUserId()
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
    [SwaggerResponse(StatusCodes.Status200OK, "Поле обновлено")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Недостаточно прав")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Аккаунт или валюта не найдены")]
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
            OwnerId = GetCurrentUserId()
        };

        var result = await mediator.Send(command, cancellationToken);
        return ApiResult.FromCommandResult(result);
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Удалить счет(закрыть)",
        Description = "Удаляет счет, если его баланс равен нулю. Для всех типов счетов баланс должен быть равен нулю.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Счет успешно удален")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации или ненулевой баланс", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Недостаточно прав")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Аккаунт не найден")]
    public async Task<IActionResult> DeleteAccount(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteAccountCommand
        {
            AccountId = id,
            OwnerId = GetCurrentUserId()
        };

        var result = await mediator.Send(command, cancellationToken);
        return ApiResult.FromCommandResult(result);
    }
}