using AccountService.Common.Extensions;
using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;
using AccountService.Infrastructure.Data;
using MediatR;
using Messaging.Events;
using Messaging.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Features.Accounts.Commands.AccrueInterest;

public class AccrueInterestHandler(
    IAccountRepository accountRepository,
    ApplicationDbContext context,
    IOutboxService outboxService)
    : IRequestHandler<AccrueInterestCommand, CommandResult<Guid>>
{
    public async Task<CommandResult<Guid>> Handle(AccrueInterestCommand request, CancellationToken ct)
    {
        await accountRepository.BeginTransactionAsync(ct);

        try
        {
            var account = await accountRepository.GetAccountByIdAsync(request.AccountId, ct);

            if (account == null)
            {
                await accountRepository.RollbackAsync(ct);
                return CommandResult<Guid>.Failure(404, $"Счёт с ID {request.AccountId} не найден");
            }

            if (account.Type != AccountType.Deposit)
            {
                await accountRepository.RollbackAsync(ct);
                return CommandResult<Guid>.Failure(400, "Начисление процентов доступно только для депозитных счетов");
            }

            if (account.IsDeleted)
            {
                await accountRepository.RollbackAsync(ct);
                return CommandResult<Guid>.Failure(410, "Счёт удалён");
            }

            if (account.InterestRate is null or <= 0)
            {
                await accountRepository.RollbackAsync(ct);
                return CommandResult<Guid>.Failure(400, "У счёта не установлена процентная ставка");
            }

            await context.Database.ExecuteSqlRawAsync("SELECT accrue_interest({0})", request.AccountId);

            var interestAccrued = new InterestAccrued(account.Id,
                DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), DateOnly.FromDateTime(DateTime.Today), 0);
            await outboxService.AddAsync(interestAccrued, ct);

            await accountRepository.CommitAsync(ct);
            return CommandResult<Guid>.Success();
        }
        catch (Exception pgEx) when (pgEx.IsConcurrencyException())
        {
            await accountRepository.RollbackAsync(ct);
            return CommandResult<Guid>.Failure(409, "Конфликт параллелизма. Попробуйте выполнить операцию позже.");
        }
        catch (Exception ex)
        {
            await accountRepository.RollbackAsync(ct);
            return CommandResult<Guid>.Failure(500, $"Ошибка при начислении процентов: {ex.Message}");
        }
    }
}