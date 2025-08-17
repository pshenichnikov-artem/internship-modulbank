using Messaging.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Messaging.Interfaces;

public interface IMessagingDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; set; }
    DbSet<InboxConsumed> InboxConsumed { get; set; }
    DbSet<InboxDeadLetter> InboxDeadLetters { get; set; }
    DbSet<AuditEvent> AuditEvents { get; set; }

    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}