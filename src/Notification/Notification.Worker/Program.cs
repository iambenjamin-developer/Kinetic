using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notification.Infrastructure;
using Notification.Worker.Consumers;

namespace Notification.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();


            // Add PostgreSQL DbContext
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<NotificationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // ⬇️ Configurar MassTransit
            builder.Services.AddMassTransit(x =>
            {
                x.AddConsumer<ProductCreatedConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {   /*Nombre que tiene en el docker-compose 'rabbitmq', si probamos en local 'localhost'*/
                    cfg.Host("localhost", "/", h =>
                    {
                        h.Username("rabbitAdmin");
                        h.Password("secretPassword");
                    });

                    cfg.ReceiveEndpoint("product-created-queue", e =>
                    {
                        e.ConfigureConsumer<ProductCreatedConsumer>(context);
                    });
                });
            });

            /*
              // Add RabbitMQ - MassTransit
            builder.Services.AddMassTransit(x =>
            {
                x.AddConsumer<ProductMessageConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(builder.Configuration["RabbitMQ:Url"]);
                    cfg.ReceiveEndpoint("notification-service-subscriber", e =>
                    {
                        e.UseMessageRetry(r =>
                        {
                            r.Interval(10, TimeSpan.FromSeconds(2));
                        });
                        e.ConfigureConsumer<ProductMessageConsumer>(context);
                        e.UseConcurrencyLimit(1);
                    });
                });
            });

            //services.AddMassTransitHostedService();
             
             */


            var host = builder.Build();
            host.Run();
        }
    }
}