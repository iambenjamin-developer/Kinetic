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

### Ejemplos de Uso

#### **Crear un Producto**
```json
POST /api/products
{
  "name": "Producto de Prueba",
  "description": "Descripción del producto",
  "price": 99.99,
  "categoryId": 1
}
```

---

```mermaid
graph TD
    A[Inventory API] --> B[GET /api/products]
    A --> C[GET /api/products/:id]
    A --> D[POST /api/products]
    A --> E[PUT /api/products/:id]
    A --> F[DELETE /api/products/:id]

    D --> G[ProductCreated Event]
    E --> H[ProductUpdated Event]
    F --> I[ProductDeleted Event]

    G --> J[RabbitMQ]
    H --> J
    I --> J
```

---

## Flujo de Datos

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

## Patrones de Resiliencia Implementados

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

## Componentes del Sistema de Mensajes Pendientes

```mermaid
graph LR
    subgraph "Entidades"
        A[PendingMessage]
        B[Product]
        C[Category]
    end
    
    subgraph "Servicios"
        D[PendingMessageService]
        E[ResilientMessagePublisher]
        F[PendingMessageProcessorService]
    end
    
    subgraph "Interfaces"
        G[IPendingMessageService]
        H[IResilientMessagePublisher]
        I[IResiliencePolicy]
    end
    
    subgraph "Políticas"
        J[Timeout Policy]
        K[Circuit Breaker Policy]
    end
    
    subgraph "Infraestructura"
        L[InventoryDbContext]
        M[RabbitMQ]
    end
    
    A --> D
    D --> G
    E --> H
    F --> G
    E --> I
    I --> J
    I --> K
    D --> L
    E --> M
    F --> M
```

## Estados de los Mensajes

```mermaid
stateDiagram-v2
    [*] --> Pending: Mensaje creado
    Pending --> Processing: Background service lo toma
    Processing --> Processed: Envío exitoso
    Processing --> Pending: Error en envío
    Pending --> Failed: Máximo de reintentos alcanzado
    Processed --> [*]: Limpieza automática
    Failed --> [*]: Requiere intervención manual
```


### Monitoreo de Eventos
Después de ejecutar las pruebas, puedes verificar que los eventos se procesaron correctamente:

1. **RabbitMQ Management UI**: `http://localhost:15672`
   - Usuario: `rabbitAdmin`
   - Contraseña: `secretPassword`

2. **Base de datos de notificaciones**:
   ```sql
   SELECT * FROM "InventoryEventLogs" ORDER BY "ReceivedAt" DESC;
   ```

### Notas Importantes
- La colección incluye ejemplos de datos para cada endpoint
- Los IDs se generan automáticamente por la base de datos
- Los eventos se procesan de forma asíncrona por el Notification.Worker
- Puedes usar la colección para probar el manejo de errores y reintentos

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


## Resumen de Componentes y Responsabilidades

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