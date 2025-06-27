using MassTransit;
using SharedKernel.Contracts;

namespace Notification.Worker.Consumers
{
    public class ProductUpdatedConsumer : IConsumer<ProductUpdated>
    {
        public Task Consume(ConsumeContext<ProductUpdated> context)
        {
            var msg = context.Message;
            Console.WriteLine($"[Updated] Producto {msg.Id} actualizado a: {msg.Name} con stock {msg.Stock}");
            return Task.CompletedTask;
        }
    }

}
