using System.Reflection;
using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;
using MediatR;

namespace AccountService.Features.Accounts.Commands.UpdateAccountField;

public class UpdateAccountFieldHandler(IAccountRepository accountRepository)
    : IRequestHandler<UpdateAccountFieldCommand, CommandResult<object>>
{
    public async Task<CommandResult<object>> Handle(UpdateAccountFieldCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var account = await accountRepository.GetAccountByIdAsync(request.Id);
            if (account == null)
                return CommandResult<object>.Failure(404, $"Счет с ID {request.Id} не найден");

            var property = typeof(Account).GetProperty(request.FieldName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property == null)
                return CommandResult<object>.Failure(400, $"Поле {request.FieldName} не существует");

            if (!property.CanWrite)
                return CommandResult<object>.Failure(400, $"Поле {request.FieldName} не может быть изменено");

            try
            {
                var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var typedValue = Convert.ChangeType(request.FieldValue, targetType);
                property.SetValue(account, typedValue);
            }
            catch (Exception)
            {
                return CommandResult<object>.Failure(400, $"Неверный формат значения для поля {request.FieldName}");
            }

            await accountRepository.UpdateAccountAsync(account);
            return CommandResult<object>.Success();
        }
        catch (Exception ex)
        {
            return CommandResult<object>.Failure(500, ex.Message);
        }
    }
}