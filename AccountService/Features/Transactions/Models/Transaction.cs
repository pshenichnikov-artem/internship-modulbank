using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AccountService.Features.Accounts.Model;

namespace AccountService.Features.Transactions.Models;

public class Transaction
{
    [Key] public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    [ForeignKey(nameof(AccountId))] public Account? Account { get; set; }

    public Guid? CounterpartyAccountId { get; set; }

    [Required] public decimal Amount { get; set; }

    [Required] public string Currency { get; set; } = string.Empty;

    public TransactionType Type { get; set; }

    [StringLength(500)] public string Description { get; set; } = string.Empty;

    [Required] public DateTime Timestamp { get; set; }


    public bool IsCanceled { get; set; } = false;
    public DateTime? CanceledAt { get; set; } = null;


    public uint Version { get; set; }
}