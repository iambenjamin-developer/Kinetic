using MassTransit;
using SharedKernel.Events;

namespace Notification.Worker.Consumers
{
    public class ProductCreatedConsumer : IConsumer<ProductCreated>
    {
        private readonly ILogger<ProductCreatedConsumer> _logger;

        public ProductCreatedConsumer(ILogger<ProductCreatedConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<ProductCreated> context)
        {
            var message = context.Message;
            _logger.LogInformation($"\n{message.Id}\n{{\r\n  \"name\": \"{message.Name}\",\r\n  \"description\": \"{message.Description}\",\r\n  \"price\": {message.Price},\r\n  \"stock\": {message.Stock},\r\n  \"categoryId\": {message.Category}\r\n}}");

            // Aquí podrías guardar en DB o enviar notificaciones reales
            return Task.CompletedTask;
        }
    }
}
