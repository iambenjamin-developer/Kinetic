# Resumen: Mecanismo de Reintentos en MassTransit

## ✅ ¿Qué hemos implementado?

### 1. **Configuración de Reintentos**
- **3 reintentos** con intervalo de **5 segundos** entre cada intento
- Configurado en `Notification.Worker/Program.cs` para todas las colas

### 2. **Simulación de Errores**
- Código habilitado en `ProductCreatedConsumer` para simular fallos
- Falla los primeros 3 intentos, luego procesa exitosamente

### 3. **Consumer de Errores**
- `ErrorConsumer` para procesar mensajes que terminan en cola de error
- Guarda información detallada de errores en la base de datos

### 4. **Scripts de Prueba**
- `test-retry-scenario.ps1`: Prueba reintentos exitosos
- `test-error-scenario.ps1`: Prueba escenario de error persistente

## 🔄 Flujo de Manejo de Errores

```
1. Mensaje llega a product-created-queue
2. Consumer intenta procesar → FALLA (simulado)
3. MassTransit espera 5 segundos
4. Reintenta → FALLA (simulado)
5. MassTransit espera 5 segundos
6. Reintenta → FALLA (simulado)
7. MassTransit mueve el mensaje a product-created-queue_error
8. ErrorConsumer procesa el mensaje de error
```

## 📁 Archivos Modificados/Creados

### Archivos Modificados:
- `src/Notification/Notification.Worker/Consumers/ProductCreatedConsumer.cs`
- `src/Notification/Notification.Worker/Program.cs`

### Archivos Creados:
- `src/Notification/Notification.Worker/Consumers/ErrorConsumer.cs`
- `test-retry-scenario.ps1`
- `test-error-scenario.ps1`
- `TESTING-RETRY-MECHANISM.md`
- `RETRY-MECHANISM-SUMMARY.md`

## 🚀 Cómo Probar

### Prueba Básica (Reintentos Exitosos):
```powershell
# 1. Iniciar servicios
docker-compose up -d
cd src/Inventory/Inventory.API && dotnet run
cd src/Notification/Notification.Worker && dotnet run

# 2. Ejecutar test
.\test-retry-scenario.ps1
```

### Prueba de Error Persistente:
```powershell
# 1. Modificar ProductCreatedConsumer para que siempre falle
# 2. Reiniciar Notification.Worker
# 3. Ejecutar test
.\test-error-scenario.ps1
```

## 📊 Qué Verificar

### 1. **Logs del Notification.Worker**
```
warn: === Intento #0 de procesar el Product Producto Test ===
warn: === Intento #1 de procesar el Product Producto Test ===
warn: === Intento #2 de procesar el Product Producto Test ===
info: === Producto procesado exitosamente en intento #3 ===
```

### 2. **RabbitMQ Management UI** (http://localhost:15672)
- `product-created-queue`: Cola principal
- `product-created-queue_error`: Cola de errores (si aplica)

### 3. **Base de Datos de Notification**
```sql
-- Ver eventos procesados
SELECT * FROM "InventoryEventLogs" ORDER BY "ReceivedAt" DESC LIMIT 5;

-- Ver errores
SELECT * FROM "InventoryEventLogs" WHERE "EventType" LIKE '%ERROR%';
```

## ⚙️ Configuración Actual

### Reintentos:
```csharp
e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
```

### Simulación de Errores:
```csharp
if (retryAttempt < 3) // Falla los primeros 3 intentos
{
    throw new Exception("=== Fallo simulado para probar reintentos ===");
}
```

## 🔧 Personalización

### Cambiar Número de Reintentos:
```csharp
e.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(10))); // 5 reintentos cada 10s
```

### Reintentos Exponenciales:
```csharp
e.UseMessageRetry(r => r.Exponential(4, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(8)));
```

### Filtro de Excepciones:
```csharp
e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5))
    .Handle<TimeoutException>()
    .Ignore<ArgumentException>());
```

## 🧹 Limpieza

### Deshabilitar Simulación:
```csharp
// Comentar estas líneas en ProductCreatedConsumer
/*
var retryAttempt = context.GetRetryAttempt();
if (retryAttempt < 3)
{
    throw new Exception("=== Fallo simulado para probar reintentos ===");
}
*/
```

### Limpiar Colas:
- RabbitMQ Management UI → Queues → Purge

### Limpiar Datos:
```sql
DELETE FROM "InventoryEventLogs" WHERE "Payload" LIKE '%Test%';
```

## 📈 Monitoreo

### Métricas Importantes:
- Tasa de éxito vs fallos
- Tiempo promedio de procesamiento
- Número de mensajes en cola de error
- Frecuencia de reintentos

### Alertas Recomendadas:
- Mensajes en cola de error > 10
- Tiempo de procesamiento > 30 segundos
- Tasa de fallos > 20%

---

**✅ El sistema está listo para probar el manejo de errores y reintentos de MassTransit!** 