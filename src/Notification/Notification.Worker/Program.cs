using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notification.Infrastructure;
using Notification.Worker.Consumers;
using RabbitMQ.Client;

namespace Notification.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Registrar servicio Worker
            builder.Services.AddHostedService<Worker>();

            // Configurar DbContext
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<NotificationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Configurar MassTransit con RabbitMQ
            builder.Services.AddMassTransit(x =>
            {
                x.AddConsumer<ProductCreatedConsumer>();
                x.AddConsumer<ProductUpdatedConsumer>();
                x.AddConsumer<ProductDeletedConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                { /*Nombre que tiene en el docker-compose 'rabbitmq', si probamos en local 'localhost'*/
                    cfg.Host("localhost", "/", h =>
                    {
                        h.Username("rabbitAdmin");
                        h.Password("secretPassword");
                    });

                    // Exchange y colas manualmente asociadas
                    const string exchangeName = "inventory_exchange";

                    cfg.ReceiveEndpoint("product-created-queue", e =>
                    {
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5))); // 3 reintentos cada 5s
                        e.ConfigureConsumer<ProductCreatedConsumer>(context);
                        e.Bind(exchangeName, s =>
                        {
                            s.RoutingKey = "product.created";
                            s.ExchangeType = ExchangeType.Direct;
                        });
                    });

                    cfg.ReceiveEndpoint("product-updated-queue", e =>
                    {
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5))); // 3 reintentos cada 5s
                        e.ConfigureConsumer<ProductUpdatedConsumer>(context);
                        e.Bind(exchangeName, s =>
                        {
                            s.RoutingKey = "product.updated";
                            s.ExchangeType = ExchangeType.Direct;
                        });
                    });

                    cfg.ReceiveEndpoint("product-deleted-queue", e =>
                    {
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5))); // 3 reintentos cada 5s
                        e.ConfigureConsumer<ProductDeletedConsumer>(context);
                        e.Bind(exchangeName, s =>
                        {
                            s.RoutingKey = "product.deleted";
                            s.ExchangeType = ExchangeType.Direct;
                        });
                    });
                });
            });

            var host = builder.Build();

            // Ejecutar migraciones de EF Core si es necesario
            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
                db.Database.Migrate();
            }

            host.Run();
        }
    }
}
