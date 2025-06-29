# Testing del Mecanismo de Reintentos en MassTransit

## ¿Cómo funciona el manejo de errores?

### 1. **Configuración actual**
- **Reintentos**: 3 intentos
- **Intervalo**: 5 segundos entre reintentos
- **Cola de error**: Se crea automáticamente como `{queue-name}_error`

### 2. **Flujo de procesamiento**
```
1. Mensaje llega a product-created-queue
2. Consumer intenta procesar → FALLA (simulado)
3. MassTransit espera 5 segundos
4. Reintenta → FALLA (simulado)
5. MassTransit espera 5 segundos
6. Reintenta → FALLA (simulado)
7. MassTransit mueve el mensaje a product-created-queue_error
```

## Pasos para probar

### Paso 1: Iniciar los servicios
```bash
# Terminal 1: Iniciar RabbitMQ y PostgreSQL
docker-compose up -d

# Terminal 2: Iniciar Inventory.API
cd src/Inventory/Inventory.API
dotnet run

# Terminal 3: Iniciar Notification.Worker
cd src/Notification/Notification.Worker
dotnet run
```

### Paso 2: Ejecutar el test
```powershell
# Ejecutar el script de prueba
.\test-retry-scenario.ps1
```

### Paso 3: Verificar los resultados

#### A. En los logs del Notification.Worker
Deberías ver algo como:
```
warn: Notification.Worker.Consumers.ProductCreatedConsumer[0]
      === Intento #0 de procesar el Product Producto Test Reintentos ===
warn: Notification.Worker.Consumers.ProductCreatedConsumer[0]
      === Intento #1 de procesar el Product Producto Test Reintentos ===
warn: Notification.Worker.Consumers.ProductCreatedConsumer[0]
      === Intento #2 de procesar el Product Producto Test Reintentos ===
info: Notification.Worker.Consumers.ProductCreatedConsumer[0]
      === Producto Producto Test Reintentos procesado exitosamente en intento #3 ===
```

#### B. En RabbitMQ Management UI (http://localhost:15672)
1. Login: `rabbitAdmin` / `secretPassword`
2. Ir a "Queues"
3. Verificar que existen:
   - `product-created-queue`
   - `product-created-queue_error` (si hay errores persistentes)

#### C. En la base de datos de Notification
```sql
-- Verificar que el mensaje se procesó finalmente
SELECT * FROM "InventoryEventLogs" 
WHERE "EventType" = 'ProductCreated' 
ORDER BY "ReceivedAt" DESC 
LIMIT 5;
```

## Escenarios de prueba adicionales

### Escenario 1: Error persistente (más de 3 intentos)
Modifica el consumer para que siempre falle:
```csharp
// Cambiar esta línea:
if (retryAttempt < 3) // Simular fallos en los primeros 3 intentos

// Por esta:
if (retryAttempt < 10) // Siempre falla
```

**Resultado esperado**: El mensaje termina en `product-created-queue_error`

### Escenario 2: Error intermitente
```csharp
// Simular error solo en intentos pares
if (retryAttempt % 2 == 0) // Falla en intentos 0, 2, 4...
{
    throw new Exception("Error intermitente");
}
```

### Escenario 3: Error de base de datos
```csharp
// Simular error de conexión a BD
if (retryAttempt < 2)
{
    throw new InvalidOperationException("Error de conexión a base de datos");
}
```

## Configuración avanzada de reintentos

### Personalizar reintentos por endpoint
```csharp
cfg.ReceiveEndpoint("product-created-queue", e =>
{
    // Reintentos exponenciales: 1s, 2s, 4s, 8s
    e.UseMessageRetry(r => r.Exponential(4, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(8)));
    
    // O reintentos con filtro de excepciones
    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5))
        .Handle<TimeoutException>()
        .Ignore<ArgumentException>());
        
    e.ConfigureConsumer<ProductCreatedConsumer>(context);
});
```

### Configurar cola de error personalizada
```csharp
cfg.ReceiveEndpoint("product-created-queue", e =>
{
    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
    e.UseInMemoryOutbox(); // Opcional: para mensajes que no se pueden procesar
    
    // Configurar endpoint de error personalizado
    e.ConfigureMessageTopology();
    
    e.ConfigureConsumer<ProductCreatedConsumer>(context);
});
```

## Monitoreo y debugging

### 1. Habilitar logs detallados de MassTransit
```json
{
  "Logging": {
    "LogLevel": {
      "MassTransit": "Debug",
      "Default": "Information"
    }
  }
}
```

### 2. Verificar métricas en RabbitMQ
- Mensajes en cola principal
- Mensajes en cola de error
- Tasa de procesamiento
- Tiempo de procesamiento

### 3. Consultas útiles para debugging
```sql
-- Ver todos los eventos procesados
SELECT "EventType", COUNT(*) as Total, 
       MIN("ReceivedAt") as FirstEvent,
       MAX("ReceivedAt") as LastEvent
FROM "InventoryEventLogs" 
GROUP BY "EventType"
ORDER BY "EventType";

-- Ver eventos de las últimas 24 horas
SELECT * FROM "InventoryEventLogs" 
WHERE "ReceivedAt" >= NOW() - INTERVAL '24 hours'
ORDER BY "ReceivedAt" DESC;
```

## Limpieza después de las pruebas

### 1. Deshabilitar simulación de errores
```csharp
// Comentar o eliminar el código de simulación
/*
var retryAttempt = context.GetRetryAttempt();
if (retryAttempt < 3)
{
    throw new Exception("=== Fallo simulado para probar reintentos ===");
}
*/
```

### 2. Limpiar colas de error
En RabbitMQ Management UI:
1. Ir a la cola `product-created-queue_error`
2. Hacer clic en "Purge" para limpiar mensajes de error

### 3. Limpiar datos de prueba
```sql
-- Eliminar eventos de prueba
DELETE FROM "InventoryEventLogs" 
WHERE "Payload" LIKE '%Producto Test Reintentos%';
``` 