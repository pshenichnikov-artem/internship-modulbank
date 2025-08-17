using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AccountService.Features.Transactions.Models;

namespace AccountService.Features.Accounts.Model;

public class Account
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public AccountType Type { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal? InterestRate { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public List<Transaction> Transactions { get; set; } = [];
    public bool IsDeleted { get; set; }
    public bool IsFrozen { get; set; } = false;


    //ReSharper disable once UnusedMember.Global Используется в процедуре StoredProcedures
    public DateTime? LastInterestAccrual { get; set; } = null;

    [NotMapped] [Timestamp] public uint Version { get; set; }
}