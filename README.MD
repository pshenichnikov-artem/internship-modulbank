# Internship-Modulbank

Репозиторий с микросервисом управления банковскими счетами и транзакциями — **AccountService**.

Основная документация и подробное описание сервиса находятся в каталоге [`AccountService/README.md`](./AccountService/README.md).  
Информация о тестах находится в [`AccountServiceTests/README.md`](./AccountServiceTests/README.md)

## Быстрый старт

1. Клонируйте репозиторий:
```bash
git clone https://github.com/pshenichnikov-artem/internship-modulbank.git
```
2. Запустите проект с помощью Docker Compose:
```bash
docker-compose up --build
```

## Запуск тестов
1. Перейдите в каталог AccountServiceTests:
```bash
cd .\AccountServiceTests\
```
2. Запустите тест:
```bash
dotnet test
```
