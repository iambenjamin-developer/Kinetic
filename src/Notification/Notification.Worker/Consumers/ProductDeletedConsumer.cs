using MassTransit;
using SharedKernel.Contracts;

namespace Notification.Worker.Consumers
{
    public class ProductDeletedConsumer : IConsumer<ProductDeleted>
    {
        public Task Consume(ConsumeContext<ProductDeleted> context)
        {
            Console.WriteLine($"[Deleted] Producto eliminado: {context.Message.Id}");
            return Task.CompletedTask;
        }
    }
}
