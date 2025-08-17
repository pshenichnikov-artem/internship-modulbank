# Messaging Library

Библиотека для работы с асинхронными сообщениями в микросервисной архитектуре на основе RabbitMQ.

## Возможности

- **Outbox Pattern** - гарантированная доставка сообщений с транзакционной консистентностью
- **Inbox Pattern** - защита от дублирования сообщений
- **Dead Letter Queue** - обработка ошибочных сообщений
- **Audit Trail** - логирование всех событий
- **Retry с экспоненциальной задержкой** - устойчивость к сбоям
- **Корреляция сообщений** - связывание запросов

## Быстрый старт

### 1. Подключение библиотеки

```csharp
builder.Services
    .UseMessaging<ApplicationDbContext>(builder.Configuration, opt =>
    {
        opt.ServiceName = "account-service";
        opt.Exchange = "account.events";
        //Включает все функции
        opt.UseFullMessaging();      
    });
```

### 2. Настройка конфигурации

```json
{
  "Messaging": {
    "ServiceName": "account-service",
    "Exchange": "account.events"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "User": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

### 3. Создание события

```csharp
[RoutingKey("client.blocked")]
public record ClientBlocked(Guid ClientId) : IEvent;
```

### 4. Отправка сообщений

```csharp
public class AccountService
{
    private readonly IMessagePublisher _publisher;
    private readonly IMessageBroker _broker;

    public async Task BlockClientAsync(Guid clientId)
    {
        var blockedEvent = new ClientBlocked(clientId);
        // Добавляет в Outbox
        await _publisher.AddAsync(blockedEvent, cancellationToken);
    }

    public async Task SendDirectAsync(Guid clientId)
    {
        var blockedEvent = new ClientBlocked(clientId);
        // Сразу отправляет в RabbitMQ
        await _broker.PublishAsync(blockedEvent, cancellationToken);
    }
}
```

### 5. Создание консьюмера

```csharp
public class AuditConsumerService : BaseConsumer
{
    public AuditConsumerService(
        IServiceProvider sp,
        ILogger<AuditConsumerService> log,
        IConnectionFactory factory,
        ServerSettings settings)
        : base(sp, log, factory, "account.audit", settings)
    {
    }

    protected override string HandlerName => "AuditConsumer";

    protected override async Task HandlePayloadAsync(
        IServiceProvider scope,
        string routingKey,
        MessageEnvelope envelope,
        CancellationToken ct)
    {
        var auditService = scope.GetRequiredService<IAuditService>();
        await auditService.LogEventAsync(envelope, ct);
    }
}
```

## Архитектура

### Outbox Pattern
- Сообщения сохраняются в БД в той же транзакции
- `OutboxDispatcherService` отправляет их в RabbitMQ
- Гарантирует доставку при сбоях

### Inbox Pattern
- Проверка дублирования по `EventId` + `HandlerName`
- Идемпотентная обработка сообщений
- Защита от повторной обработки

### Dead Letter Queue
- Ошибочные сообщения сохраняются в БД
- Логирование причин ошибок

## Миграции БД

Для добавления таблиц Messaging в ваш DbContext, вызовите метод в `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Добавляет таблицы Messaging
    modelBuilder.AddMessagingEntities();
}
```

**Создаваемые таблицы:**
- `outbox_messages` - исходящие сообщения
- `inbox_consumed` - обработанные сообщения
- `inbox_dead_letters` - ошибочные сообщения
- `audit_events` - аудит событий

## Корреляция запросов (CorrelationId)

Библиотека автоматически обрабатывает корреляцию запросов для сквозного отслеживания:

### Генерация CorrelationId
- При отправке сообщения через `IMessagePublisher.AddAsync()` автоматически генерируется или извлекается из HTTP контекста
- Если CorrelationId отсутствует, создается новый `Guid.NewGuid().ToString()`
- CausationId всегда генерируется новый для каждого сообщения

### CorrelationMiddleware
- Извлекает `X-Correlation-Id` и `X-Causation-Id` из HTTP заголовков
- Сохраняет их в `HttpContext.Items` для использования в приложении
- Добавляет заголовки в HTTP ответ для продолжения цепочки отслеживания
