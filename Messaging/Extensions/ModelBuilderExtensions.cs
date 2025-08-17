using Messaging.Entities;
using Microsoft.EntityFrameworkCore;

namespace Messaging.Extensions;

public static class ModelBuilderExtensions
{
    public static ModelBuilder AddMessagingEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.ServiceName, x.PublishedAtUtc });
            b.HasIndex(x => x.CreatedAtUtc);
            b.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<InboxConsumed>(b =>
        {
            b.ToTable("inbox_consumed");
            b.HasKey(x => new { x.MessageId, x.ServiceName, x.Handler });
        });

        modelBuilder.Entity<InboxDeadLetter>(b =>
        {
            b.ToTable("inbox_dead_letters");
            b.HasKey(x => x.MessageId);
            b.HasIndex(x => new { x.ServiceName, x.ReceivedAtUtc });
        });

        modelBuilder.Entity<AuditEvent>(b =>
        {
            b.ToTable("audit_events");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.ServiceName, x.ReceivedAtUtc });
        });

        return modelBuilder;
    }
}
