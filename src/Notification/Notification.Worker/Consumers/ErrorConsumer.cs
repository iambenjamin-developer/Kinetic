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

            // Guardar en tabla específica de errores
            var errorLog = new ErrorLog
            {
                EventType = nameof(ProductCreated),
                OriginalQueue = "product-created-queue",
                RoutingKey = "product.created",
                Payload = JsonSerializer.Serialize(message),
                RetryAttempts = 3,
                ErrorMessage = "Mensaje falló después de todos los reintentos",
                ErrorTime = DateTime.UtcNow,
                IsResolved = false
            };

            _context.ErrorLogs.Add(errorLog);
            await _context.SaveChangesAsync();

            _logger.LogError($"=== Error guardado en ErrorLogs con ID: {errorLog.Id} ===");  
        }
    }
}