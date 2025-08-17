using Messaging.Entities;
using Messaging.Extensions;
using Messaging.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MessagingLibraryTests;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options), IMessagingDbContext
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<InboxConsumed> InboxConsumed { get; set; }
    public DbSet<InboxDeadLetter> InboxDeadLetters { get; set; }
    public DbSet<AuditEvent> AuditEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddMessagingEntities();
    }
}