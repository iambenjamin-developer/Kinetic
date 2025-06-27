using System.Text.Json;
using MassTransit;
using Notification.Domain.Entities;
using Notification.Infrastructure;
using SharedKernel.Contracts;

namespace Notification.Worker.Consumers
{
    public class ProductCreatedConsumer : IConsumer<ProductCreated>
    {
        private readonly ILogger<ProductCreatedConsumer> _logger;
        private readonly NotificationDbContext _context;

        public ProductCreatedConsumer(ILogger<ProductCreatedConsumer> logger, NotificationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task Consume(ConsumeContext<ProductCreated> context)
        {
            var retryAttempt = context.GetRetryAttempt();
            if (retryAttempt < 3) // Simular fallos en los primeros 3 intentos
            {
                _logger.LogWarning($"=== Intento #{retryAttempt} de procesar el Product {context.Message.Name} ===");
                throw new Exception("=== Fallo simulado para probar reintentos ===");
            }

            var message = context.Message;

            var payload = JsonSerializer.Serialize(context.Message);
            // Aquí podrías guardar en DB o enviar notificaciones reales
            var log = new InventoryEventLog
            {
                EventType = nameof(ProductCreated),
                Payload = payload,
                ReceivedAt = DateTime.UtcNow
            };

            _context.InventoryEventLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"=== Producto {context.Message.Name} procesado ===");
            //_logger.LogInformation(payload);
        }
    }
}
