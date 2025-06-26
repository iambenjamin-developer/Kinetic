using MassTransit;
using SharedKernel.Models;

namespace Inventory.API.Producers
{
    public class QueueProducerService
    {
        readonly IBus _bus;

        public QueueProducerService(IBus bus)
        {
            _bus = bus;
        }

        public async Task<bool> SendSubscribeProductEvent(/*ProductDto productDto*/)
        {
            try
            {
                //var productMessage = new ProductMessage()
                //{
                //    Name = productDto.Name,
                //    Description = productDto.Description,
                //    Price = productDto.Price,
                //    Stock = productDto.Stock,
                //    Category = productDto.Category.Name,
                //};


                var productMessage = new ProductMessage()
                {
                    Name = "Martillo",
                    Description = "Martillo de carpintero",
                    Price = 99.9M,
                    Stock = 7,
                    Category = "Herramientas",
                };

                await _bus.Publish(productMessage);

                return true;
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }

    }
}
