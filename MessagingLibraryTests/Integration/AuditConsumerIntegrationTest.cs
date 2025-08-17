using System.Text;
using Messaging.Events;
using Messaging.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Xunit;

namespace MessagingLibraryTests.Integration;

public class AuditConsumerIntegrationTest(MessagingTestFixture fixture) : BaseMessagingIntegrationTest(fixture)
{
    [Theory]
    [InlineData("account.opened")]
    [InlineData("money.credited")]
    [InlineData("money.debited")]
    [InlineData("client.blocked")]
    [InlineData("client.unblocked")]
    [InlineData("money.transfer")]
    public async Task AuditConsumer_ShouldStoreAllMessageTypes(string routingKey)
    {
        using var cleanupScope = Services.CreateScope();
        var cleanupContext = cleanupScope.ServiceProvider.GetRequiredService<IMessagingDbContext>();
        cleanupContext.InboxConsumed.RemoveRange(cleanupContext.InboxConsumed);
        await cleanupContext.SaveChangesAsync();

        using var scope1 = Services.CreateScope();
        var factory = scope1.ServiceProvider.GetRequiredService<IConnectionFactory>();

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        var testMessage = new { Message = $"Audit test for {routingKey}" };
        var eventId = Guid.NewGuid();
        var envelope = MessageEnvelope.Create(
            eventId,
            DateTime.UtcNow,
            "test-service",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            JsonConvert.SerializeObject(testMessage)
        );

        var messageBody = Encoding.UTF8.GetBytes(envelope.ToJson());

        await channel.BasicPublishAsync($"{QueuePrefix}.events", routingKey, messageBody);

        await Task.Delay(5000);

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();

        var auditRecord = context.InboxConsumed
            .FirstOrDefault(x => x.MessageId == eventId);

        Assert.NotNull(auditRecord);
        Assert.Equal("AuditConsumerService", auditRecord.Handler);
    }
}