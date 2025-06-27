using MassTransit;
using SharedKernel.Contracts;

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
            _logger.LogInformation($"\n{{\n  \"id\": \"{message.Id}\",\r\n  \"name\": \"{message.Name}\",\r\n  \"description\": \"{message.Description}\",\r\n  \"price\": {message.Price},\r\n  \"stock\": {message.Stock},\r\n  \"category\": {message.Category}\r\n}}\n");

            // Aquí podrías guardar en DB o enviar notificaciones reales
            return Task.CompletedTask;
        }
    }
}
