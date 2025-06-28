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

            // Worker en segundo plano
            builder.Services.AddHostedService<Worker>();

            // Configuración de la base de datos
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<NotificationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Configuración de MassTransit + RabbitMQ
            builder.Services.AddMassTransit(x =>
            {
                // Registrar consumidores
                x.AddConsumer<ProductCreatedConsumer>();
                x.AddConsumer<ProductUpdatedConsumer>();
                x.AddConsumer<ProductDeletedConsumer>();

                // Configurar transporte RabbitMQ
                x.UsingRabbitMq((context, cfg) =>
                {
                    var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
                    var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "rabbitAdmin";
                    var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "secretPassword";

                    cfg.Host(rabbitHost, "/", h =>
                    {
                        h.Username(rabbitUser);
                        h.Password(rabbitPass);
                    });

                    // Exchange y colas manualmente asociadas
                    const string exchangeName = "inventory_exchange";

                    // ---- Cola: producto creado ----
                    cfg.ReceiveEndpoint("product-created-queue", e =>
                    {
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5))); // 3 reintentos cada 5s
                        e.ConfigureConsumer<ProductCreatedConsumer>(context);
                        e.Bind(exchangeName, b =>
                        {
                            b.ExchangeType = ExchangeType.Direct;
                            b.RoutingKey = "product.created";
                        });
                    });

                    // ---- Cola: producto actualizado ----
                    cfg.ReceiveEndpoint("product-updated-queue", e =>
                    {
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5))); // 3 reintentos cada 5s
                        e.ConfigureConsumer<ProductUpdatedConsumer>(context);
                        e.Bind(exchangeName, b =>
                        {
                            b.ExchangeType = ExchangeType.Direct;
                            b.RoutingKey = "product.updated";
                        });
                    });

                    // ---- Cola: producto eliminado ----
                    cfg.ReceiveEndpoint("product-deleted-queue", e =>
                    {
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5))); // 3 reintentos cada 5s
                        e.ConfigureConsumer<ProductDeletedConsumer>(context);
                        e.Bind(exchangeName, b =>
                        {
                            b.ExchangeType = ExchangeType.Direct;
                            b.RoutingKey = "product.deleted";
                        });
                    });
                });
            });

            var host = builder.Build();

            // Migraciones EF Core
            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
                db.Database.Migrate();
            }

            host.Run();
        }
    }
}
