namespace Messaging.Configuration;

public class ServerSettings
{
    public string ServiceName { get; init; } = null!;
    public string Exchange { get; init; } = null!;
    public string AuditQueueName { get; init; } = null!;
}
