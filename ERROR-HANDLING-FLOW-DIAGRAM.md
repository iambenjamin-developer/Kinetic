# Diagrama: Flujo de Manejo de Errores en Cola de Creación de Productos

## 🔄 **Flujo Completo de Manejo de Errores**

```mermaid
graph TD
    %% Publisher (Inventory.API)
    A[📤 Inventory.API<br/>ProductController] -->|Publish ProductCreated| B[📡 Exchange<br/>inventory_exchange]
    
    %% Exchange y Routing
    B -->|Routing Key: product.created| C[📥 Cola Normal<br/>product-created-queue]
    
    %% Consumer Normal con Reintentos
    C --> D[🔄 ProductCreatedConsumer<br/>Intento #1]
    D -->|❌ FALLA| E[⏱️ MassTransit<br/>Espera 5 segundos]
    E --> F[🔄 ProductCreatedConsumer<br/>Intento #2]
    F -->|❌ FALLA| G[⏱️ MassTransit<br/>Espera 5 segundos]
    G --> H[🔄 ProductCreatedConsumer<br/>Intento #3]
    H -->|❌ FALLA| I[🚨 MassTransit<br/>Mueve a Cola de Error]
    
    %% Cola de Error
    I --> J[📥 Cola de Error<br/>product-created-queue_error]
    J --> K[⚠️ ProductCreatedConsumerError<br/>Procesa Error]
    
    %% Base de Datos
    K --> L[💾 ErrorLogs Table<br/>Guarda Error]
    
    %% Estados del Mensaje
    M[📋 Estados del Mensaje] --> M1[🟢 Enviado]
    M --> M2[🟡 En Procesamiento]
    M --> M3[🟠 Reintentando]
    M --> M4[🔴 En Cola de Error]
    M --> M5[⚫ Procesado como Error]
    
    %% Componentes del Sistema
    N[🏗️ Componentes del Sistema] --> N1[📤 Publisher<br/>Inventory.API]
    N --> N2[📡 Message Broker<br/>RabbitMQ]
    N --> N3[🔄 Consumer<br/>Notification.Worker]
    N --> N4[💾 Database<br/>PostgreSQL]
    N --> N5[⚠️ Error Handler<br/>ErrorConsumer]
    
    %% Estilos
    classDef publisher fill:#e1f5fe
    classDef exchange fill:#f3e5f5
    classDef queue fill:#e8f5e8
    classDef consumer fill:#fff3e0
    classDef error fill:#ffebee
    classDef database fill:#f1f8e9
    classDef state fill:#fce4ec
    classDef component fill:#e0f2f1
    
    class A publisher
    class B exchange
    class C,J queue
    class D,F,H consumer
    class K error
    class L database
    class M,M1,M2,M3,M4,M5 state
    class N,N1,N2,N3,N4,N5 component
```

## 📊 **Estados del Mensaje**

```mermaid
stateDiagram-v2
    [*] --> Enviado: Publisher envía mensaje
    
    Enviado --> EnProcesamiento: Llega a cola normal
    EnProcesamiento --> Reintentando: Consumer falla
    
    Reintentando --> EnProcesamiento: Reintento exitoso
    Reintentando --> EnColaDeError: Agotan reintentos
    
    EnColaDeError --> ProcesadoComoError: ErrorConsumer procesa
    EnProcesamiento --> ProcesadoExitoso: Consumer exitoso
    
    ProcesadoComoError --> [*]
    ProcesadoExitoso --> [*]
    
    note right of Enviado
        📤 Inventory.API publica
        ProductCreated
    end note
    
    note right of EnProcesamiento
        📥 product-created-queue
        ProductCreatedConsumer
    end note
    
    note right of Reintentando
        🔄 3 reintentos
        ⏱️ 5s entre intentos
    end note
    
    note right of EnColaDeError
        📥 product-created-queue_error
        ProductCreatedConsumerError
    end note
    
    note right of ProcesadoComoError
        💾 ErrorLogs table
        ⚠️ Error registrado
    end note
```

## 🏗️ **Componentes del Sistema de Mensajes Pendientes**

```mermaid
graph TB
    subgraph "📤 Publisher Layer"
        A1[Inventory.API<br/>ProductController]
        A2[ResilientMessagePublisher]
        A3[PendingMessageService]
    end
    
    subgraph "📡 Message Broker Layer"
        B1[RabbitMQ<br/>inventory_exchange]
        B2[product-created-queue]
        B3[product-created-queue_error]
    end
    
    subgraph "🔄 Consumer Layer"
        C1[Notification.Worker]
        C2[ProductCreatedConsumer]
        C3[ProductCreatedConsumerError]
    end
    
    subgraph "💾 Database Layer"
        D1[Inventory Database<br/>PendingMessages]
        D2[Notification Database<br/>ErrorLogs]
        D3[Notification Database<br/>InventoryEventLogs]
    end
    
    subgraph "⚠️ Error Handling Layer"
        E1[Retry Policy<br/>3 intentos, 5s]
        E2[Error Consumer<br/>Procesa fallos]
        E3[Error Logging<br/>Guarda errores]
    end
    
    %% Conexiones Publisher
    A1 --> A2
    A2 --> B1
    A2 --> A3
    A3 --> D1
    
    %% Conexiones Message Broker
    B1 --> B2
    B2 --> C2
    B2 --> B3
    B3 --> C3
    
    %% Conexiones Consumer
    C1 --> C2
    C1 --> C3
    C2 --> D3
    C3 --> D2
    
    %% Conexiones Error Handling
    E1 --> C2
    E2 --> C3
    E3 --> D2
    
    %% Estilos
    classDef publisher fill:#e1f5fe
    classDef broker fill:#f3e5f5
    classDef consumer fill:#fff3e0
    classDef database fill:#f1f8e9
    classDef error fill:#ffebee
    
    class A1,A2,A3 publisher
    class B1,B2,B3 broker
    class C1,C2,C3 consumer
    class D1,D2,D3 database
    class E1,E2,E3 error
```

## 🔍 **Detalle del Flujo de Error**

```mermaid
sequenceDiagram
    participant API as 📤 Inventory.API
    participant Exchange as 📡 Exchange
    participant Queue as 📥 product-created-queue
    participant Consumer as 🔄 ProductCreatedConsumer
    participant ErrorQueue as ⚠️ product-created-queue_error
    participant ErrorConsumer as 🚨 ProductCreatedConsumerError
    participant DB as 💾 ErrorLogs
    
    Note over API,DB: Flujo Normal (Sin Errores)
    API->>Exchange: Publish ProductCreated
    Exchange->>Queue: Route with key "product.created"
    Queue->>Consumer: Consume message
    Consumer->>DB: Save to InventoryEventLogs
    Consumer->>Queue: Acknowledge success
    
    Note over API,DB: Flujo con Errores
    API->>Exchange: Publish ProductCreated
    Exchange->>Queue: Route with key "product.created"
    
    Queue->>Consumer: Consume message (Intento #1)
    Consumer->>Consumer: ❌ Simulated Error
    Consumer->>Queue: Negative acknowledge
    
    Note over Queue: ⏱️ Wait 5 seconds
    Queue->>Consumer: Consume message (Intento #2)
    Consumer->>Consumer: ❌ Simulated Error
    Consumer->>Queue: Negative acknowledge
    
    Note over Queue: ⏱️ Wait 5 seconds
    Queue->>Consumer: Consume message (Intento #3)
    Consumer->>Consumer: ❌ Simulated Error
    Consumer->>Queue: Negative acknowledge
    
    Note over Queue: 🚨 Move to error queue
    Queue->>ErrorQueue: Move failed message
    ErrorQueue->>ErrorConsumer: Consume error message
    ErrorConsumer->>DB: Save to ErrorLogs
    ErrorConsumer->>ErrorQueue: Acknowledge error
```

## 📈 **Métricas y Monitoreo**

```mermaid
graph LR
    subgraph "📊 Métricas de Cola"
        M1[Mensajes Enviados]
        M2[Mensajes Procesados]
        M3[Mensajes en Error]
        M4[Tiempo de Procesamiento]
    end
    
    subgraph "🚨 Alertas"
        A1[Cola de Error > 10 mensajes]
        A2[Tiempo > 30 segundos]
        A3[Tasa de Error > 20%]
        A4[Consumer offline]
    end
    
    subgraph "📋 Dashboard"
        D1[Estado de Colas]
        D2[Errores por Tipo]
        D3[Reintentos Promedio]
        D4[Errores No Resueltos]
    end
    
    M1 --> D1
    M2 --> D1
    M3 --> D2
    M4 --> D3
    A1 --> D4
    A2 --> D4
    A3 --> D4
    A4 --> D4
```

## 🎯 **Resumen del Sistema**

### **Componentes Principales:**
1. **📤 Publisher**: Inventory.API que publica eventos
2. **📡 Message Broker**: RabbitMQ con exchange y colas
3. **🔄 Consumer**: Notification.Worker que procesa mensajes
4. **⚠️ Error Handler**: Sistema de manejo de errores
5. **💾 Database**: Almacenamiento de eventos y errores

### **Estados del Mensaje:**
1. **🟢 Enviado**: Publisher envía mensaje
2. **🟡 En Procesamiento**: Consumer intenta procesar
3. **🟠 Reintentando**: MassTransit reintenta (3 veces)
4. **🔴 En Cola de Error**: Mensaje falló después de reintentos
5. **⚫ Procesado como Error**: ErrorConsumer procesa el error

### **Flujo de Error:**
1. Consumer falla → MassTransit reintenta (3x, 5s)
2. Si todos fallan → Mensaje va a cola de error
3. ErrorConsumer procesa → Guarda en ErrorLogs
4. Sistema puede reintentar manualmente o notificar

**Este sistema garantiza que ningún mensaje se pierda y que todos los errores sean registrados y manejados apropiadamente.** 