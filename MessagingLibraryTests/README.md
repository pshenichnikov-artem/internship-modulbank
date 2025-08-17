# MessagingServiceTests

Набор тестов для проверки функциональности библиотеки Messaging.

## Связанные тесты

- **AccountServiceTests** - тесты основного микросервиса: [`../AccountServiceTests/README.md`](../AccountServiceTests/README.md)
  - Интеграционные тесты с использованием библиотеки Messaging
  - Тестирование отправки событий через AccountService

---

## Структура тестов

### Integration Tests

Интеграционные тесты с реальными контейнерами PostgreSQL и RabbitMQ:

**AuditConsumerIntegrationTest** — тестирование консьюмера аудита событий

**OutboxIntegrationTest** — тестирование Outbox Pattern и диспетчера сообщений

**Особенности:**
- Используются реальные Docker-контейнеры PostgreSQL и RabbitMQ
- Автоматическое создание и очистка тестовой среды
- Тестирование устойчивости к сбоям RabbitMQ

**Основные тесты:**

- **AuditConsumer_ShouldStoreAllMessageTypes** (AuditConsumerIntegrationTest)  
  Проверка обработки всех типов событий консьюмером аудита с валидацией сохранения в InboxConsumed.

- **OutboxDispatcher_ShouldPublishWhenRabbitAvailable** (OutboxIntegrationTest)  
  Проверка успешной отправки сообщений через Outbox Pattern при доступном RabbitMQ.

- **OutboxDispatcher_ShouldRetryWhenRabbitUnavailable** (OutboxIntegrationTest)  
  Проверка механизма повторов при недоступности RabbitMQ с валидацией ошибок и счетчика попыток.

- **OutboxDispatcher_ShouldRecoverAfterRabbitRestart** (OutboxIntegrationTest)  
  Проверка восстановления отправки сообщений после перезапуска RabbitMQ.

- **OutboxDispatcher_ShouldHandleInvalidJson** (OutboxIntegrationTest)  
  Проверка обработки некорректных JSON сообщений с блокировкой после 10 ошибок формата.

---

## Запуск тестов

**Требования:** Docker для создания тестовых контейнеров

```bash
dotnet test
```

## Технологии

- **XUnit** — фреймворк для тестирования
- **Microsoft.AspNetCore.Mvc.Testing** — тестирование веб-приложений
- **Moq** — библиотека для создания mock-объектов
- **Microsoft.EntityFrameworkCore.InMemory** — база данных в памяти для тестов
- **Testcontainers.PostgreSql** — запуск PostgreSQL в Docker-контейнере для интеграционных тестов
- **Testcontainers.RabbitMq** — запуск RabbitMQ в Docker-контейнере для тестирования сообщений