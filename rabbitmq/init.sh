#!/bin/bash
set -e

# Запускаем RabbitMQ в фоне
rabbitmq-server &
RABBITMQ_PID=$!

# Ждем запуска RabbitMQ
sleep 10

# Ждем готовности RabbitMQ
rabbitmqctl wait /var/lib/rabbitmq/mnesia/rabbit@$(hostname).pid

# Импортируем definitions
rabbitmqctl import_definitions /tmp/definitions.json

echo "RabbitMQ definitions imported successfully"

# Ждем завершения процесса RabbitMQ
wait $RABBITMQ_PID