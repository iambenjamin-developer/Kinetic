## Arquitectura General de la Solución

```mermaid
graph TB
    subgraph "Cliente"
        Client[Cliente HTTP]
    end
    
    subgraph "Inventory.API (Productor)"
        API[API REST]
        ProductService[ProductService]
        ResilientPublisher[ResilientMessagePublisher]
        PendingService[PendingMessageService]
        BackgroundProcessor[Background Processor]
    end
    
    subgraph "Shared Kernel"
        Contracts[Event Contracts]
        ProductCreated[ProductCreated]
        ProductUpdated[ProductUpdated]
        ProductDeleted[ProductDeleted]
    end
    
    subgraph "Notification.Worker (Consumidor)"
        Worker[Worker Service]
        ProductCreatedConsumer[ProductCreatedConsumer]
        ProductUpdatedConsumer[ProductUpdatedConsumer]
        ProductDeletedConsumer[ProductDeletedConsumer]
    end
    
    subgraph "Infraestructura"
        RabbitMQ[RabbitMQ]
        InventoryDB[(Inventory DB)]
        NotificationDB[(Notification DB)]
    end
    
    Client --> API
    API --> ProductService
    ProductService --> ResilientPublisher
    ResilientPublisher --> RabbitMQ
    ResilientPublisher --> PendingService
    PendingService --> InventoryDB
    BackgroundProcessor --> PendingService
    BackgroundProcessor --> RabbitMQ
    
    RabbitMQ --> Worker
    Worker --> ProductCreatedConsumer
    Worker --> ProductUpdatedConsumer
    Worker --> ProductDeletedConsumer
    
    ProductCreatedConsumer --> NotificationDB
    ProductUpdatedConsumer --> NotificationDB
    ProductDeletedConsumer --> NotificationDB
    
    Contracts --> API
    Contracts --> Worker
```

## Flujo de Patrones de Mensajería Implementados

```mermaid
sequenceDiagram
    participant Client
    participant API as Inventory.API
    participant DB as Inventory DB
    participant Publisher as ResilientPublisher
    participant RabbitMQ
    participant Worker as Notification.Worker
    participant NotifDB as Notification DB
    
    Note over Client,NotifDB: Flujo Normal
    Client->>API: POST /api/products
    API->>DB: Save Product
    API->>Publisher: Publish Event
    Publisher->>RabbitMQ: Publish Message
    RabbitMQ->>Worker: Deliver Message
    Worker->>NotifDB: Save Event Log
    API-->>Client: 201 Created
    
    Note over Client,NotifDB: Flujo con Fallo RabbitMQ
    Client->>API: POST /api/products
    API->>DB: Save Product
    API->>Publisher: Publish Event
    Publisher->>RabbitMQ: Publish (FAILS)
    Publisher->>DB: Save as Pending
    API-->>Client: 503/504 (Mensaje guardado)
    
    Note over Client,NotifDB: Recuperación Automática
    loop Cada 30 segundos
        Publisher->>DB: Get Pending Messages
        Publisher->>RabbitMQ: Publish Pending
        Publisher->>DB: Mark as Processed
    end
```

## Flujo de Patrones de Resiliencia Implementados

```mermaid
graph LR
    subgraph "Políticas de Resiliencia"
        CB[Circuit Breaker<br/>2 fallos → 8s abierto]
        TO[Timeout<br/>10 segundos]
        RETRY[Retry Policy<br/>3 intentos cada 5s]
    end
    
    subgraph "Componentes"
        PUBLISHER[ResilientMessagePublisher]
        PENDING[PendingMessageService]
        PROCESSOR[Background Processor]
    end
    
    subgraph "Estados"
        CLOSED[Circuito Cerrado]
        OPEN[Circuito Abierto]
    end
    
    PUBLISHER --> CB
    PUBLISHER --> TO
    PUBLISHER --> PENDING
    PROCESSOR --> PENDING
    
    CB --> CLOSED
    CB --> OPEN
```

## Caso de Uso: Cuando se cae RabbitMQ

```mermaid
graph TD
    subgraph "Tablas Afectadas"
        subgraph "Inventory DB"
            Products[Products<br/>✅ Normal]
            Categories[Categories<br/>✅ Normal]
            PendingMessages[PendingMessages<br/>📈 Aumenta]
        end
        
        subgraph "Notification DB"
            InventoryEventLogs[InventoryEventLogs<br/>❌ No recibe]
            ErrorLogs[ErrorLogs<br/>❌ No recibe]
        end
    end
    
    subgraph "Comportamiento del Sistema"
        API[Inventory.API<br/>✅ Sigue funcionando]
        Worker[Notification.Worker<br/>❌ No procesa]
        Background[Background Processor<br/>⏳ Espera RabbitMQ]
    end
    
    subgraph "Recuperación"
        RabbitMQ[RabbitMQ<br/>🔄 Se recupera]
        Background --> RabbitMQ
        Background --> PendingMessages
        PendingMessages --> InventoryEventLogs
    end
    
    style PendingMessages fill:#ffcc99
    style InventoryEventLogs fill:#ff9999
    style ErrorLogs fill:#ff9999
    style Worker fill:#ff9999
```

## Caso de Uso: Cuando la cola genera error

```mermaid
graph TD
    subgraph "Flujo de Mensaje"
        Message[Message llega a Worker]
        Consumer[Consumer procesa]
        Success{Procesamiento exitoso?}
    end
    
    subgraph "Flujo Exitoso"
        SaveLog[Guardar en InventoryEventLogs]
        SuccessResponse[Procesado correctamente]
    end
    
    subgraph "Flujo de Error"
        Error{Error en procesamiento?}
        Retry{Retry < 3?}
        ErrorQueue[Error Queue]
        ErrorLog[ErrorLog Table]
        NoSaveLog[NO se guarda en InventoryEventLogs]
    end
    
    subgraph "Tablas Afectadas"
        subgraph "Notification DB"
            InventoryEventLogs[InventoryEventLogs<br/>Se guarda en exito<br/>NO se guarda en error]
            ErrorLogs[ErrorLogs<br/>Se incrementa solo en error]
        end
        
        subgraph "RabbitMQ"
            MainQueue[Main Queue<br/>Mensaje removido]
            ErrorQueue[Error Queue<br/>Mensaje agregado solo en error]
        end
    end
    
    Message --> Consumer
    Consumer --> Success
    
    Success -->|Si| SaveLog
    SaveLog --> SuccessResponse
    SaveLog --> InventoryEventLogs
    
    Success -->|No| Error
    Error -->|Si| Retry
    Retry -->|Si| Consumer
    Retry -->|No| ErrorQueue
    ErrorQueue --> ErrorLog
    ErrorQueue --> NoSaveLog
    ErrorLog --> ErrorLogs
    NoSaveLog --> InventoryEventLogs
    
    style ErrorLogs fill:#ffcc99
    style ErrorQueue fill:#ffcc99
    style NoSaveLog fill:#ff9999
    style SaveLog fill:#99ff99
    style SuccessResponse fill:#99ff99
```

---

## Limpieza de Docker y Levantamiento del Entorno Local

> Ejecutar desde la raíz del proyecto, donde se encuentra el archivo `docker-compose.yml`.

### 1. Detener y eliminar recursos de Docker Compose

```
docker-compose down --volumes --remove-orphans
```

### 2. Limpiar recursos de Docker no utilizados

```
docker container prune -f      # Eliminar contenedores detenidos
docker volume prune -f         # Eliminar volúmenes no utilizados
docker network prune -f        # Eliminar redes no utilizadas
docker image prune -a -f       # (Opcional) Eliminar imágenes no utilizadas
```

### 3. Levantar el entorno con nuevo build

```
docker-compose up -d --build
```

---


## Endpoints de la API

[http://localhost:5000/swagger/index.html](http://localhost:5000/swagger/index.html)


## Pruebas con Postman

### Descargar la Colección

1. **Descargar la colección**: [Kinetic.postman_collection.json](Kinetic.postman_collection.json)
2. **Importar en Postman**:
   - Abrir Postman
   - Hacer clic en "File" y luego "Import"
   - Seleccionar el archivo `Kinetic.postman_collection.json`
   - La colección se importará automáticamente

### Endpoints Incluidos en la Colección

#### **Productos**
- `GET /api/products` - Obtener todos los productos
- `GET /api/products/{id}` - Obtener producto por ID
- `POST /api/products` - Crear nuevo producto
- `PUT /api/products/{id}` - Actualizar producto
- `DELETE /api/products/{id}` - Eliminar producto

#### **Categorías**
- `GET /api/categories` - Obtener todas las categorías

---

## Características Principales

- **API REST completa** con todos los endpoints requeridos
- **Integración con RabbitMQ** usando exchange direct
- **Circuit Breaker + Timeout** para resiliencia
- **Persistencia de mensajes** para evitar pérdidas
- **Procesamiento automático** de mensajes pendientes
- **Docker Compose** para el ambiente completo
- **Documentación Swagger** incluida
- **Manejo de errores** y reintentos
- **Arquitectura limpia** con separación de responsabilidades 

## Beneficios del Sistema

- **No pérdida de mensajes** cuando RabbitMQ está caído
- **Procesamiento automático** cuando el servicio se recupera
- **Reintentos inteligentes** con límite configurable
- **Monitoreo detallado** con logs estructurados
- **Limpieza automática** de mensajes procesados
- **Escalabilidad** con procesamiento en background
- **Resiliencia** con políticas de timeout y circuit breaker 

---

## Migraciones EF Core (en modo desarrollo)
#### Setear  como proyecto principal API y en Package Manager Console (apuntando a Infrastructure) ejecutar los siguientes comandos:
```

Add-Migration Initial -Context InventoryDbContext -OutputDir Migrations
Update-Database  -Context InventoryDbContext
Remove-Migration -Context InventoryDbContext
```

#### Setear  como proyecto principal Worker y en Package Manager Console (apuntando a Infrastructure) ejecutar los siguientes comandos:
```
Add-Migration Initial -Context NotificationDbContext -OutputDir Migrations
Update-Database  -Context NotificationDbContext
Remove-Migration -Context NotificationDbContext
```
