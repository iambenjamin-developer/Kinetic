using System.Text.Json;
using MassTransit;
using Notification.Domain.Entities;
using Notification.Infrastructure;
using SharedKernel.Contracts;

namespace Notification.Worker.Consumers
{
    public class ProductDeletedConsumer : IConsumer<ProductDeleted>
    {
        private readonly ILogger<ProductDeletedConsumer> _logger;
        private readonly NotificationDbContext _context;

        public ProductDeletedConsumer(ILogger<ProductDeletedConsumer> logger, NotificationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task Consume(ConsumeContext<ProductDeleted> context)
        {
            var payload = JsonSerializer.Serialize(context.Message);

            var log = new InventoryEventLog
            {
                EventType = nameof(ProductDeleted),
                Payload = payload,
                ReceivedAt = DateTime.UtcNow
            };

            _context.InventoryEventLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation(payload);
        }
    }
}
