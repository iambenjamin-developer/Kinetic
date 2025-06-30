# Diagramas de Arquitectura - Kinetic

## 1. Arquitectura General de la Solución

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

## 2. Flujo de Patrones de Mensajería Implementados

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

## 3. Flujo de Patrones de Resiliencia Implementados

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
        HALF[Circuito Semi-Abierto]
    end
    
    PUBLISHER --> CB
    PUBLISHER --> TO
    PUBLISHER --> PENDING
    PROCESSOR --> PENDING
    
    CB --> CLOSED
    CB --> OPEN
    CB --> HALF
    
    style CB fill:#ff9999
    style TO fill:#99ccff
    style RETRY fill:#99ff99
```

## 4. Caso de Uso: Cuando se cae RabbitMQ

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

## 5. Caso de Uso: Cuando la cola genera error

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

## 6. Resumen de Componentes y Responsabilidades

```mermaid
graph TB
    subgraph "Inventory.API (Productor)"
        API[API REST]
        ProductService[ProductService]
        ResilientPublisher[ResilientMessagePublisher]
        PendingService[PendingMessageService]
        BackgroundProcessor[Background Processor]
    end
    
    subgraph "Notification.Worker (Consumidor)"
        Worker[Worker Service]
        Consumers[Consumers]
        RetryPolicy[Retry Policy]
    end
    
    subgraph "Shared Kernel"
        Contracts[Event Contracts]
    end
    
    subgraph "Infraestructura"
        RabbitMQ[RabbitMQ]
        InventoryDB[(Inventory DB)]
        NotificationDB[(Notification DB)]
    end
    
    subgraph "Patrones Implementados"
        CircuitBreaker[Circuit Breaker]
        Timeout[Timeout Policy]
        MessagePersistence[Message Persistence]
        AutomaticRetry[Automatic Retry]
        ErrorHandling[Error Handling]
    end
    
    API --> ProductService
    ProductService --> ResilientPublisher
    ResilientPublisher --> CircuitBreaker
    ResilientPublisher --> Timeout
    ResilientPublisher --> MessagePersistence
    MessagePersistence --> PendingService
    BackgroundProcessor --> AutomaticRetry
    
    Worker --> Consumers
    Consumers --> RetryPolicy
    Consumers --> ErrorHandling
    
    style CircuitBreaker fill:#ff9999
    style Timeout fill:#99ccff
    style MessagePersistence fill:#99ff99
    style AutomaticRetry fill:#ffcc99
    style ErrorHandling fill:#ff9999
``` 