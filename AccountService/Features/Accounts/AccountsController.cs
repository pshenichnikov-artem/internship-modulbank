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
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AccountsController(IMediator mediator) : ControllerBase
{
    [HttpPost("search")]
    [SwaggerOperation(Summary = "Получить список счетов",
        Description = "Позволяет фильтровать и сортировать счета по владельцам, валютам, типам и статусу")]
    [SwaggerResponse(StatusCodes.Status200OK, "Список счетов", typeof(IEnumerable<AccountDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка запроса", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
    public async Task<IActionResult> GetAccounts(
        GetAccountsQuery query)
    {
        var result = await mediator.Send(query);
        return ApiResult.FromCommandResult(result);
    }

    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Получить счет по ID")]
    [SwaggerResponse(StatusCodes.Status200OK, "Счет найден", typeof(AccountDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Счет не найден", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Неверный ID", typeof(ErrorResponse))]
    public async Task<IActionResult> GetAccount(
        [FromRoute] Guid id,
        [FromQuery] List<string>? fields = null)
    {
        var query = new GetAccountByIdQuery(id, fields);
        var result = await mediator.Send(query);
        return ApiResult.FromCommandResult(result);
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Создать новый счет")]
    [SwaggerResponse(StatusCodes.Status201Created, "Счет создан", typeof(AccountDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Клиент не найден", typeof(ErrorResponse))]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountCommand request)
    {
        var result = await mediator.Send(request);
        return ApiResult.FromCommandResult(result, nameof(GetAccount),
            new { id = result.IsSuccess ? result.Data : Guid.Empty });
    }

    [HttpPut("{id}")]
    [SwaggerOperation(Summary = "Обновить существующий счет")]
    [SwaggerResponse(StatusCodes.Status200OK, "Обновление выполнено", typeof(AccountDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Счет не найден", typeof(ErrorResponse))]
    public async Task<IActionResult> UpdateAccount(
        [FromRoute] Guid id,
        [FromBody] UpdateAccountCommand request)
    {
        request.Id = id;
        var result = await mediator.Send(request);
        return ApiResult.FromCommandResult(result);
    }

    [HttpPatch("{id}/{fieldName}")]
    [SwaggerOperation(
        Summary = "Обновить отдельное поле счета",
        Description =
            "Допустимые поля: 'Currency' — трехбуквенный код валюты, 'InterestRate' — процентная ставка"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Поле обновлено")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка запроса", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Счет не найден", typeof(ErrorResponse))]
    public async Task<IActionResult> UpdateAccountField(
        [FromRoute] Guid id,
        [FromRoute] string fieldName,
        [FromBody] JsonElement fieldValue)
    {
        object? typedValue = fieldName.ToLower() switch
        {
            "currency" => fieldValue.ValueKind == JsonValueKind.String
                ? fieldValue.GetString()?.ToUpperInvariant()
                : null,

            "interestrate" => fieldValue.ValueKind == JsonValueKind.Number && fieldValue.TryGetDecimal(out var rate)
                ? rate
                : null,

            "closedat" => fieldValue.ValueKind == JsonValueKind.String && fieldValue.TryGetDateTime(out var dt)
                ? dt
                : null,

            _ => null
        };

        if (typedValue is null)
            return ApiResult.BadRequest($"Невозможно распарсить поле {fieldName}");

        var command = new UpdateAccountFieldCommand
        {
            Id = id,
            FieldName = fieldName,
            FieldValue = typedValue
        };

        var result = await mediator.Send(command);
        return ApiResult.FromCommandResult(result);
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Удалить счет",
        Description = "Удаляет счет, если его баланс равен нулю. Для всех типов счетов баланс должен быть равен нулю.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Счет успешно удален")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации или ненулевой баланс", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Счет не найден", typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAccount([FromRoute] Guid id)
    {
        var command = new DeleteAccountCommand
        {
            AccountId = id
        };

        var result = await mediator.Send(command);
        return ApiResult.FromCommandResult(result);
    }
}