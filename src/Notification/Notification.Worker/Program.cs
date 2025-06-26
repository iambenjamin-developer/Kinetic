using MassTransit;
using Notification.Worker.Consumers;

namespace Notification.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();


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

            var host = builder.Build();
            host.Run();
        }
    }
}