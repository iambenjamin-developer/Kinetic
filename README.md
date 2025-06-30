# Kinetic - Sistema de Notificaciones de Inventario - Arquitectura

## Diagrama de Arquitectura General

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

# Limpieza de Docker y Levantamiento del Entorno Local

> Ejecutar desde la raíz del proyecto, donde se encuentra el archivo `docker-compose.yml`.

---

### 1. Detener y eliminar recursos de Docker Compose

```
docker-compose down --volumes --remove-orphans
```

---

### 2. Limpiar recursos de Docker no utilizados

```
docker container prune -f      # Eliminar contenedores detenidos
docker volume prune -f         # Eliminar volúmenes no utilizados
docker network prune -f        # Eliminar redes no utilizadas
docker image prune -a -f       # (Opcional) Eliminar imágenes no utilizadas
```

---

### 3. Levantar el entorno con nuevo build

```
docker-compose up -d --build
```

---




## Endpoints de la API

[http://localhost:5000/swagger/index.html](http://localhost:5000/swagger/index.html)

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

## Pruebas con Postman

### Descargar la Colección
Para facilitar las pruebas de la API, hemos creado una colección de Postman que incluye todos los endpoints disponibles.

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