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

