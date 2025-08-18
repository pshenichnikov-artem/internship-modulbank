# Internship-Modulbank

Репозиторий с микросервисом управления банковскими счетами и транзакциями — **AccountService**.

## Документация

- **AccountService** - основной микросервис: [`AccountService/README.md`](./AccountService/README.md)
- **Messaging Library** - библиотека для асинхронных сообщений: [`Messaging/README.md`](./Messaging/README.md)
- **Тесты AccountService** - интеграционные и unit тесты: [`AccountServiceTests/README.md`](./AccountServiceTests/README.md)
- **Тесты Messaging Library** - тесты библиотеки сообщений: [`MessagingLibraryTests/README.md`](./MessagingLibraryTests/README.md)

## Быстрый старт

1. Клонируйте репозиторий:
```bash
git clone https://github.com/pshenichnikov-artem/internship-modulbank.git
```
2. Запустите проект с помощью Docker Compose:
```bash
docker-compose up --build
```

## Доступы к сервисам

**RabbitMQ Management UI:** http://localhost:15672
- Логин: `admin`
- Пароль: `admin`

**Keycloak Admin Console:** http://localhost:8080
- Логин: `admin`
- Пароль: `admin123`
- *Примечание: этот аккаунт является только административным и не подходит для авторизации в Swagger. Чтобы авторизоваться в Swagger, нужно создать пользовательский аккаунт: нажмите Authorize в Swagger UI(http://localhost), на странице Keycloak выберите Register и зарегистрируйте новый аккаунт.*

## Запуск тестов
**Требуется Docker** для создания тестовых контейнеров PostgreSQL и RabbitMQ.

1. Перейдите в каталог AccountServiceTests:
```bash
cd .\AccountServiceTests\
```
2. Запустите тест:
```bash
dotnet test
```
