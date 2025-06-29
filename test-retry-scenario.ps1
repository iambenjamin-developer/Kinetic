# Script para probar el manejo de errores y reintentos en MassTransit
# Ejecutar este script después de iniciar los servicios

Write-Host "=== Probando manejo de errores y reintentos ===" -ForegroundColor Green

# 1. Crear un producto para generar un evento ProductCreated
Write-Host "1. Creando producto para generar evento ProductCreated..." -ForegroundColor Yellow

$productData = @{
    name = "Producto Test Reintentos"
    description = "Producto para probar reintentos"
    price = 99.99
    categoryId = 1
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/products" -Method POST -Body $productData -ContentType "application/json"
    Write-Host "Producto creado con ID: $($response.id)" -ForegroundColor Green
} catch {
    Write-Host "Error creando producto: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n2. Esperando 20 segundos para ver los logs de reintentos..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

Write-Host "`n3. Verificando logs en la base de datos..." -ForegroundColor Yellow

# Aquí puedes verificar los logs en la base de datos de Notification
# SELECT * FROM "InventoryEventLogs" ORDER BY "ReceivedAt" DESC LIMIT 5;

Write-Host "`n=== Instrucciones para verificar ===" -ForegroundColor Cyan
Write-Host "1. Revisa los logs del Notification.Worker para ver los reintentos" -ForegroundColor White
Write-Host "2. Verifica en RabbitMQ Management UI las colas:" -ForegroundColor White
Write-Host "   - product-created-queue" -ForegroundColor White
Write-Host "   - product-created-queue_error (si hay errores)" -ForegroundColor White
Write-Host "3. Consulta la base de datos de Notification:" -ForegroundColor White
Write-Host "   SELECT * FROM \"InventoryEventLogs\" ORDER BY \"ReceivedAt\" DESC LIMIT 5;" -ForegroundColor White

Write-Host "`n=== Fin del test ===" -ForegroundColor Green 