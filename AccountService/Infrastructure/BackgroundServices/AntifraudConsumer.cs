using System.Text.Json;
using AccountService.Features.Accounts.Commands.FreezeAccount;
using MediatR;
using Messaging.Configuration;
using Messaging.Consumers;
using Messaging.Events;
using RabbitMQ.Client;

namespace AccountService.Infrastructure.BackgroundServices;

public sealed class AntifraudConsumer(
    IServiceProvider serviceProvider,
    ILogger<AntifraudConsumer> logger,
    IConnectionFactory connectionFactory,
    ServerSettings settings)
    : BaseConsumer(serviceProvider, logger, connectionFactory, "account.antifraud", settings)
{
    protected override string HandlerName => "AntifraudConsumer";

    protected override async Task HandlePayloadAsync(IServiceProvider scope, string routingKey,
        MessageEnvelope envelope,
        CancellationToken ct)
    {
        if (routingKey is not ("client.blocked" or "client.unblocked"))
            throw new InvalidOperationException($"Неподдерживаемый routingKey {routingKey}");

        var mediator = scope.GetRequiredService<IMediator>();
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var clientId = routingKey == "client.blocked"
            ? envelope.Payload.RootElement.Deserialize<ClientBlocked>(options)?.ClientId
              ?? throw new InvalidOperationException("Не удалось десериализовать событие ClientBlocked")
            : envelope.Payload.RootElement.Deserialize<ClientUnblocked>(options)?.ClientId
              ?? throw new InvalidOperationException("Не удалось десериализовать событие ClientUnblocked");

        var isFrozen = routingKey == "client.blocked";

        var command = new FreezeAccountCommand(clientId, isFrozen);
        await mediator.Send(command, ct);
    }
}