# Script para probar el escenario de error persistente
# Este script simula un escenario donde los mensajes terminan en la cola de error

Write-Host "=== Probando escenario de error persistente ===" -ForegroundColor Green

Write-Host "`n⚠️  IMPORTANTE: Antes de ejecutar este script:" -ForegroundColor Yellow
Write-Host "1. Modifica el ProductCreatedConsumer para que siempre falle:" -ForegroundColor White
Write-Host "   Cambia: if (retryAttempt < 3)" -ForegroundColor White
Write-Host "   Por: if (retryAttempt < 10)" -ForegroundColor White
Write-Host "2. Reinicia el Notification.Worker" -ForegroundColor White
Write-Host "3. Luego ejecuta este script" -ForegroundColor White

Write-Host "`n¿Has hecho los cambios? (y/n): " -ForegroundColor Cyan
$response = Read-Host

if ($response -ne "y") {
    Write-Host "Por favor, haz los cambios primero y luego ejecuta este script." -ForegroundColor Red
    exit 1
}

# 1. Crear un producto para generar un evento ProductCreated
Write-Host "`n1. Creando producto para generar evento ProductCreated..." -ForegroundColor Yellow

$productData = @{
    name = "Producto Error Persistente"
    description = "Producto para probar cola de error"
    price = 199.99
    categoryId = 1
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/products" -Method POST -Body $productData -ContentType "application/json"
    Write-Host "Producto creado con ID: $($response.id)" -ForegroundColor Green
} catch {
    Write-Host "Error creando producto: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n2. Esperando 30 segundos para que se agoten todos los reintentos..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

Write-Host "`n3. Verificando resultados..." -ForegroundColor Yellow

Write-Host "`n=== Instrucciones para verificar ===" -ForegroundColor Cyan
Write-Host "1. Revisa los logs del Notification.Worker:" -ForegroundColor White
Write-Host "   - Deberías ver 10 intentos fallidos" -ForegroundColor White
Write-Host "   - Al final deberías ver el ErrorConsumer procesando el mensaje" -ForegroundColor White

Write-Host "`n2. Verifica en RabbitMQ Management UI (http://localhost:15672):" -ForegroundColor White
Write-Host "   - product-created-queue: debería estar vacía" -ForegroundColor White
Write-Host "   - product-created-queue_error: debería tener 1 mensaje" -ForegroundColor White

Write-Host "`n3. Consulta la base de datos de Notification:" -ForegroundColor White
Write-Host "   SELECT * FROM \"InventoryEventLogs\" WHERE \"EventType\" LIKE '%ERROR%' ORDER BY \"ReceivedAt\" DESC;" -ForegroundColor White

Write-Host "`n4. Para procesar manualmente el mensaje de error:" -ForegroundColor White
Write-Host "   - Ve a RabbitMQ Management UI" -ForegroundColor White
Write-Host "   - En product-created-queue_error, haz clic en 'Get Message'" -ForegroundColor White
Write-Host "   - Esto activará el ErrorConsumer" -ForegroundColor White

Write-Host "`n=== Fin del test de error persistente ===" -ForegroundColor Green 