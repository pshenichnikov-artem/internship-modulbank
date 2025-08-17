#!/bin/bash
echo "Ожидание RabbitMQ"
while ! timeout 1 bash -c "echo >/dev/tcp/rabbitmq/5672" 2>/dev/null; do
    echo "RabbitMQ еще не готов, ожидание..."
    sleep 5
done
echo "RabbitMQ готов!"

echo "Ожидание Keycloak"
while ! timeout 1 bash -c "echo >/dev/tcp/keycloak/8080" 2>/dev/null; do
    echo "Keycloak еще не готов, ожидание..."
    sleep 5
done
echo "Keycloak готов!"

exec "$@"