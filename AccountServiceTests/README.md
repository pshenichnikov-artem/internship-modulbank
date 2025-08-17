# AccountServiceTests

Набор тестов для проверки функциональности AccountService.

## Связанные тесты

- **MessagingLibraryTests** - тесты библиотеки Messaging: [`../MessagingLibraryTests/README.md`](../MessagingLibraryTests/README.md)
  - `AuditConsumerIntegrationTest` - тестирование консьюмера аудита
  - `OutboxIntegrationTest` - тестирование Outbox Pattern

---

## Структура тестов

### Unit Tests

**AccountServiceUnitTests** — модульные тесты для проверки изменения валюты у счета  
**DeleteAccountHandlerTests** - модульные тесты дял проверки удаления аккаунта  
**TransactionServiceTests** - модульные тесты для проверки создания и отмены транзакций  

### Integration Tests

Интеграционные тесты с реальными контейнерами PostgreSQL и RabbitMQ:

**ParallelTransferTests** — проверка потокобезопасности и параллельных операций

**ClientBlockedEventsIntegrationTest** — тестирование отправки событий через RabbitMQ

**OutboxDispatcherIntegrationTest** — тестирование Outbox Pattern и диспетчера сообщений

**Особенности:**
- Используются реальные Docker-контейнеры PostgreSQL и RabbitMQ
- JWT-аутентификация мокнута, Keycloak не требуется
- Автоматическое создание и очистка тестовой среды

**Основные тесты:**

- **ParallelTransfers_ShouldPreserveTotalBalance** (ParallelTransferTests)  
  Одновременное выполнение 50 переводов между двумя счетами с проверкой сохранения общей суммы балансов. Проверяется корректная работа механизмов блокировки и обработка конфликтов параллелизма.

- **CancelTransactions_Parallel_CorrectBalances** (ParallelTransferTests)  
  Одновременная отмена 50 транзакций разных типов с проверкой корректности балансов и целостности данных при параллельной обработке.

- **BlockClient_ShouldSendEventToRabbitMq** (ClientBlockedEventsIntegrationTest)  
  Проверка отправки события блокировки клиента через RabbitMQ с валидацией получения сообщения и реакции на него в очереди antifraud.

- **TransferEmitsSingleEvent** (OutboxDispatcherIntegrationTest)  
  Проверка того, что перевод генерирует только одно событие TransferCompleted и оно корректно отправляется через Outbox Pattern.


---

## Запуск тестов

**Требования:** Docker для создания тестовых контейнеров

```bash
dotnet test
```

## Технологии

- **XUnit** — фреймворк для тестирования
- **Microsoft.EntityFrameworkCore.InMemory** — база данных в памяти для тестов
- **Moq** — библиотека для создания mock-объектов
- **Microsoft.Extensions.Logging** — логирование в тестах
- **Testcontainers.PostgreSql** — запуск PostgreSQL в Docker-контейнере для интеграционных тестов
- **Testcontainers.RabbitMq** — запуск RabbitMQ в Docker-контейнере для тестирования сообщений