using System.Text.Json;
using MassTransit;
using Notification.Domain.Entities;
using Notification.Infrastructure;
using SharedKernel.Contracts;

namespace Notification.Worker.Consumers
{
    /// <summary>
    /// Consumer para procesar mensajes que han fallado después de todos los reintentos
    /// Este consumer se ejecuta cuando un mensaje termina en la cola de error
    /// </summary>
    public class ErrorConsumer : IConsumer<ProductCreated>
    {
        private readonly ILogger<ErrorConsumer> _logger;
        private readonly NotificationDbContext _context;

        public ErrorConsumer(ILogger<ErrorConsumer> logger, NotificationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task Consume(ConsumeContext<ProductCreated> context)
        {
            var message = context.Message;
            var retryAttempt = context.GetRetryAttempt();

            _logger.LogError("=== MENSAJE EN COLA DE ERROR ===");
            _logger.LogError($"Producto: {message.Name}");
            _logger.LogError($"Intentos realizados: {retryAttempt}");
            _logger.LogError($"Headers: {JsonSerializer.Serialize(context.Headers)}");

            // Aquí puedes implementar lógica específica para mensajes fallidos:
            // - Enviar notificación a administradores
            // - Guardar en tabla de errores
            // - Reintentar manualmente
            // - Enviar a sistema de monitoreo

            var errorLog = new InventoryEventLog
            {
                EventType = $"{nameof(ProductCreated)}_ERROR",
                Payload = JsonSerializer.Serialize(new
                {
                    OriginalMessage = message,
                    RetryAttempts = retryAttempt,
                    ErrorTime = DateTime.UtcNow,
                    Headers = context.Headers
                }),
                ReceivedAt = DateTime.UtcNow
            };

            _context.InventoryEventLogs.Add(errorLog);
            await _context.SaveChangesAsync();

            _logger.LogError("=== Mensaje de error procesado y guardado ===");
        }
    }
} 