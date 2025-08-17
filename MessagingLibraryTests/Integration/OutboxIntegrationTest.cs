using System.Text.Json;
using Messaging.Entities;
using Messaging.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MessagingLibraryTests.Integration;

[Collection("RabbitMQ")]
[TestCaseOrderer("MessagingLibraryTests.PriorityOrderer", "MessagingLibraryTests")]
public class OutboxIntegrationTest(MessagingTestFixture fixture) : BaseMessagingIntegrationTest(fixture)
{
    [Fact]
    public async Task OutboxDispatcher_ShouldPublishWhenRabbitAvailable()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            ServiceName = QueuePrefix,
            RoutingKey = "account.opened",
            Exchange = $"{QueuePrefix}.events",
            PayloadJson = JsonSerializer.Serialize(new { Message = "Test" }),
            HeadersJson = "{}",
            PublishAttempts = 0,
            PublishedAtUtc = null
        };

        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();
            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync();
        }

        await Task.Delay(10000);

        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();
            var publishedMessage = await context.OutboxMessages.FindAsync(message.Id);
            Assert.NotNull(publishedMessage);
            Assert.NotNull(publishedMessage.PublishedAtUtc);
            Assert.True(publishedMessage.PublishAttempts > 0);
        }
    }

    [Fact, TestPriority(100)]
    public async Task OutboxDispatcher_ShouldRetryWhenRabbitUnavailable()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            ServiceName = QueuePrefix,
            RoutingKey = "money.debited",
            Exchange = $"{QueuePrefix}.events",
            PayloadJson = JsonSerializer.Serialize(new { Message = "Test" }),
            HeadersJson = "{}",
            PublishAttempts = 0,
            PublishedAtUtc = null
        };

        await Fixture.RabbitMq.ExecAsync(["rabbitmqctl", "stop_app"]);

        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();
            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync();
        }

        await Task.Delay(10000);

        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();
            var failedMessage = await context.OutboxMessages.FindAsync(message.Id);
            Assert.NotNull(failedMessage);
            Assert.Null(failedMessage.PublishedAtUtc);
            Assert.True(failedMessage.PublishAttempts > 0);
            Assert.NotNull(failedMessage.LastError);
        }

        await Fixture.RabbitMq.ExecAsync(["rabbitmqctl", "start_app"]);
    }

    [Fact, TestPriority(101)]
    public async Task OutboxDispatcher_ShouldRecoverAfterRabbitRestart()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            ServiceName = QueuePrefix,
            RoutingKey = "client.blocked",
            Exchange = $"{QueuePrefix}.events",
            PayloadJson = JsonSerializer.Serialize(new { Message = "Test" }),
            HeadersJson = "{}",
            PublishAttempts = 0,
            PublishedAtUtc = null
        };


        await Fixture.RabbitMq.ExecAsync(["rabbitmqctl", "stop_app"]);

        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();
            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync();
        }

        await Task.Delay(10000);

        await Fixture.RabbitMq.ExecAsync(["rabbitmqctl", "start_app"]);
        await Task.Delay(20000);

        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();
            var recoveredMessage = await context.OutboxMessages.FindAsync(message.Id);
            Assert.NotNull(recoveredMessage);
            Assert.NotNull(recoveredMessage.PublishedAtUtc);
            Assert.True(recoveredMessage.Status == OutboxMessageStatus.Sent);
        }
    }

    [Fact]
    public async Task OutboxDispatcher_ShouldProcessMultipleMessages()
    {
        var messages = new List<OutboxMessage>();
        var routingKeys = new[] { "account.opened", "money.debited", "client.blocked" };

        for (var i = 0; i < 3; i++)
            messages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = DateTime.UtcNow.AddSeconds(i),
                ServiceName = QueuePrefix,
                RoutingKey = routingKeys[i],
                Exchange = $"{QueuePrefix}.events",
                PayloadJson = JsonSerializer.Serialize(new { Message = $"Test {i}" }),
                HeadersJson = "{}",
                PublishAttempts = 0
            });

        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();
            context.OutboxMessages.AddRange(messages);
            await context.SaveChangesAsync();
        }

        await Task.Delay(10000);

        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();
            var publishedMessages = await context.OutboxMessages
                .Where(m => messages.Select(msg => msg.Id).Contains(m.Id))
                .ToListAsync();

            Assert.Equal(3, publishedMessages.Count);
            Assert.All(publishedMessages, m => Assert.NotNull(m.PublishedAtUtc));
        }
    }

    [Fact]
    public async Task OutboxDispatcher_ShouldHandleInvalidJson()
    {
        var invalidMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            ServiceName = QueuePrefix,
            RoutingKey = "account.invalid",
            Exchange = $"{QueuePrefix}.events",
            PayloadJson = "{ invalid json",
            HeadersJson = "{}",
            PublishAttempts = 0
        };

        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();
            context.OutboxMessages.Add(invalidMessage);
            await context.SaveChangesAsync();
        }

        await Task.Delay(20000);

        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();
            var processedMessage = await context.OutboxMessages.FindAsync(invalidMessage.Id);
            Assert.NotNull(processedMessage);
            Assert.Null(processedMessage.PublishedAtUtc);
            Assert.True(processedMessage.FormatErrorCount >= 10);
            Assert.True(processedMessage.Status == OutboxMessageStatus.Blocked);
            Assert.True(processedMessage.PublishAttempts > 0);
        }
    }
}