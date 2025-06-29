# Sistema de Notificaciones de Inventario - Arquitectura

## 🏗️ Diagrama de Arquitectura General

```mermaid
graph TB
    subgraph "Cliente"
        A[Cliente HTTP]
    end
    
    subgraph "API de Inventario (Productor)"
        B[Inventory.API]
        B1[ProductsController]
        B2[CategoriesController]
        B3[ResilientMessagePublisher]
        B4[Circuit Breaker + Timeout]
    end
    
    subgraph "Base de Datos de Inventario"
        C[(PostgreSQL - Inventory)]
        C1[Products]
        C2[Categories]
        C3[PendingMessages]
    end
    
    subgraph "Message Broker"
        D[RabbitMQ]
        D1[inventory_exchange]
        D2[product-created-queue]
        D3[product-updated-queue]
        D4[product-deleted-queue]
    end
    
    subgraph "Servicio de Notificaciones (Consumidor)"
        E[Notification.Worker]
        E1[ProductCreatedConsumer]
        E2[ProductUpdatedConsumer]
        E3[ProductDeletedConsumer]
    end
    
    subgraph "Base de Datos de Notificaciones"
        F[(PostgreSQL - Notifications)]
        F1[InventoryEventLogs]
    end
    
    A --> B
    B --> C
    B --> D
    D --> E
    E --> F
    
    B3 --> D
    B4 --> B3
```

## 🔄 Flujo de Datos

```mermaid
sequenceDiagram
    participant Client
    participant API as Inventory API
    participant DB as Inventory DB
    participant RabbitMQ
    participant Worker as Notification Worker
    participant NotifDB as Notification DB

    Note over Client,NotifDB: Flujo Normal
    Client->>API: POST /api/products
    API->>DB: Save Product
    API->>RabbitMQ: Publish ProductCreated
    RabbitMQ->>Worker: Consume Message
    Worker->>NotifDB: Save Event Log
    API-->>Client: 201 Created

    Note over Client,NotifDB: Flujo con Resiliencia
    Client->>API: POST /api/products
    API->>DB: Save Product
    API->>RabbitMQ: Publish (Circuit Breaker)
    RabbitMQ-->>API: Error/Timeout
    API->>DB: Save as Pending Message
    API-->>Client: 503/504 (Mensaje guardado)
    
    Note over Client,NotifDB: Recuperación Automática
    Worker->>DB: Check Pending Messages
    Worker->>RabbitMQ: Publish Pending Messages
    Worker->>DB: Mark as Processed
```

## 🛡️ Patrones de Resiliencia Implementados

```mermaid
graph LR
    subgraph "Políticas de Resiliencia"
        A[Circuit Breaker]
        B[Timeout Policy]
        C[Message Persistence]
        D[Automatic Retry]
    end
    
    subgraph "Componentes"
        E[ResilientMessagePublisher]
        F[PendingMessageService]
        G[Background Processor]
    end
    
    A --> E
    B --> E
    C --> F
    D --> G
    E --> F
    G --> F
```

## 📋 Endpoints de la API

```mermaid
graph TD
    A[Inventory API] --> B[GET /api/products]
    A --> C[GET /api/products/{id}]
    A --> D[POST /api/products]
    A --> E[PUT /api/products/{id}]
    A --> F[DELETE /api/products/{id}]
    
    D --> G[ProductCreated Event]
    E --> H[ProductUpdated Event]
    F --> I[ProductDeleted Event]
    
    G --> J[RabbitMQ]
    H --> J
    I --> J
```

## 🎯 Características Principales

- ✅ **API REST completa** con todos los endpoints requeridos
- ✅ **Integración con RabbitMQ** usando exchange direct
- ✅ **Circuit Breaker + Timeout** para resiliencia
- ✅ **Persistencia de mensajes** para evitar pérdidas
- ✅ **Procesamiento automático** de mensajes pendientes
- ✅ **Docker Compose** para el ambiente completo
- ✅ **Documentación Swagger** incluida
- ✅ **Manejo de errores** y reintentos
- ✅ **Arquitectura limpia** con separación de responsabilidades 