using System.Text.Json;
using MassTransit;
using Notification.Domain.Entities;
using Notification.Infrastructure;
using SharedKernel.Contracts;

namespace Notification.Worker.Consumers
{
    public class ProductUpdatedConsumer : IConsumer<ProductUpdated>
    {
        private readonly ILogger<ProductUpdatedConsumer> _logger;
        private readonly NotificationDbContext _context;

        public ProductUpdatedConsumer(ILogger<ProductUpdatedConsumer> logger, NotificationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task Consume(ConsumeContext<ProductUpdated> context)
        {
            var payload = JsonSerializer.Serialize(context.Message);

            var log = new InventoryEventLog
            {
                EventType = nameof(ProductUpdated),
                Payload = payload,
                ReceivedAt = DateTime.UtcNow
            };

            _context.InventoryEventLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation(payload);
        }
    }
}
