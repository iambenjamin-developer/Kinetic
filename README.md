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



# Kinetic

## Inventory

```
docker run --name inventory-db -e POSTGRES_USER=BenjAdmin -e POSTGRES_PASSWORD=MySecretPassword -e POSTGRES_DB=inventory-db -p 5432:5432 -d postgres:17.5-alpine
```

```
Add-Migration Initial -Context InventoryDbContext -OutputDir Migrations
Update-Database  -Context InventoryDbContext
```

## Notification

```
docker run --name notification-db -e POSTGRES_USER=BenjAdmin -e POSTGRES_PASSWORD=MySecretPassword -e POSTGRES_DB=notification-db -p 5433:5432 -d postgres:17.5-alpine
```

```
Add-Migration Initial -Context NotificationDbContext -OutputDir Migrations
Update-Database  -Context NotificationDbContext
```


/Kinetic
├── src/
│   ├── Inventory/
│   │   ├── Inventory.API/
│   │   ├── Inventory.Application/
│   │   ├── Inventory.Domain/
│   │   └── Inventory.Infrastructure/
│   └── Notification/
│       ├── Notification.Worker/
│       ├── Notification.Application/
│       ├── Notification.Domain/
│       └── Notification.Infrastructure/
├── docker-compose.yml
├── Kinetic.sln
├── README.md



🧱 Ejemplo de estructura:
/InventorySystem.sln
├── Inventory.API/
├── Inventory.Application/
├── Inventory.Infrastructure/
├── Inventory.Domain/
│
├── Notification.Worker/
├── Notification.Application/
├── Notification.Infrastructure/
├── Notification.Domain/
│
├── BuildingBlocks/ (opcional: para clases compartidas)
├── docker-compose.yml
└── README.md


🧱 Estructura recomendada para la solución Kinetic


/Kinetic
│
├── Kinetic.sln
├── docker-compose.yml
├── README.md
│
├── src/
│   ├── Inventory/
│   │   ├── Inventory.API/               --> ASP.NET Core Web API
│   │   ├── Inventory.Application/       --> lógica de aplicación (DTOs, servicios)
│   │   ├── Inventory.Domain/            --> entidades de dominio
│   │   └── Inventory.Infrastructure/    --> EF Core, RabbitMQ publisher, etc.
│   │
│   ├── Notification/
│   │   ├── Notification.Worker/         --> Worker Service .NET Core
│   │   ├── Notification.Application/    --> lógica de aplicación
│   │   ├── Notification.Domain/         --> entidades (log de eventos)
│   │   └── Notification.Infrastructure/ --> EF Core, RabbitMQ consumer, etc.
│
├── shared/                              --> (opcional) código común
│   └── Kinetic.SharedKernel/            --> clases base, excepciones, interfaces comunes
│
└── tests/                               --> (opcional) tests de unidad e integración
    ├── Inventory.Tests/
    └── Notification.Tests/


    Cómo agregar los proyectos a la solución?
Desde la raíz del proyecto (/Kinetic):

dotnet new sln -n Kinetic

# Agregar proyectos
dotnet sln add ./src/Inventory/Inventory.API/Inventory.API.csproj
dotnet sln add ./src/Inventory/Inventory.Application/Inventory.Application.csproj
dotnet sln add ./src/Inventory/Inventory.Domain/Inventory.Domain.csproj
dotnet sln add ./src/Inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj

dotnet sln add ./src/Notification/Notification.Worker/Notification.Worker.csproj
dotnet sln add ./src/Notification/Notification.Application/Notification.Application.csproj
dotnet sln add ./src/Notification/Notification.Domain/Notification.Domain.csproj
dotnet sln add ./src/Notification/Notification.Infrastructure/Notification.Infrastructure.csproj

# (opcional)
dotnet sln add ./shared/Kinetic.SharedKernel/Kinetic.SharedKernel.csproj
