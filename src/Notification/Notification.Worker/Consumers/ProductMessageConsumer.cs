using MassTransit;
using SharedKernel.Interfaces;

namespace Notification.Worker.Consumers
{
    public class ProductMessageConsumer : IConsumer<IProductMessage>
    {
        private readonly ILogger<ProductMessageConsumer> _logger;

        public ProductMessageConsumer(ILogger<ProductMessageConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<IProductMessage> context)
        {

            _logger.LogInformation($"Product: {context.Message.Name}");

            _logger.LogInformation($"Precio anterior: ${context.Message.Price}");

            _logger.LogInformation($"Precio actual(+15%): ${context.Message.Price * 1.15M}");


            return Task.CompletedTask;
        }
    }
}
