# Kinetic

docker system prune --help

docker system prune --all --volumes

[Inventario.API] -- POST /api/products
       |
    Envía mensaje a RabbitMQ (exchange: inventory_exchange, routingKey: product_created)
       |
[RabbitMQ] --> Cola: product_created
       |
[Notificaciones.Worker] -- escucha cola y guarda en su base de datos




services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest

  postgres:
    image: postgres:14
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: notificationsdb
    ports:
      - "5432:5432"

  inventory-api:
    build:
      context: ./Inventory.API
    ports:
      - "5000:80"
    depends_on:
      - rabbitmq
      - postgres

  notification-worker:
    build:
      context: ./Notification.Worker
    depends_on:
      - rabbitmq
      - postgres
