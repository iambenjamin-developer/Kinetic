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
            var message = context.Message;
            _logger.LogInformation($"\n{{\n  \"id\": \"{message.Id}\",\r\n  \"name\": \"{message.Name}\",\r\n  \"description\": \"{message.Description}\",\r\n  \"price\": {message.Price},\r\n  \"stock\": {message.Stock},\r\n  \"category\": {message.Category}\r\n}}\n");

            // Aquí podrías guardar en DB o enviar notificaciones reales
            var log = new InventoryEventLog
            {
                EventType = nameof(ProductCreated),
                Payload = JsonSerializer.Serialize(context.Message),
                ReceivedAt = DateTime.UtcNow
            };

            _context.InventoryEventLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
