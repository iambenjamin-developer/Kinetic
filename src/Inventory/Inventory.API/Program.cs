
using Inventory.API.Producers;
using Inventory.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Inventory.Application;

namespace Inventory.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add PostgreSQL DbContext
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<InventoryDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Add services to the container.


            // ➕ Agregar MassTransit con RabbitMQ
            builder.Services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {   /*Nombre que tiene en el docker-compose 'rabbitmq', si probamos en local 'localhost'*/
                    cfg.Host("localhost", "/", h =>
                    {
                        h.Username("rabbitAdmin");
                        h.Password("secretPassword");
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            /*
            // Add RabbitMQ - MassTransit
            builder.Services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(builder.Configuration["RabbitMQ:Url"]);
                    cfg.ConfigureEndpoints(context);
                });
            });
            //services.AddMassTransitHostedService();
            builder.Services.AddScoped<QueueProducerService>();
            */

            //Add Application servicesAdd commentMore actions
            builder.Services.AddApplicationServices(builder.Configuration);

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Ejecutar seed de datos (migración + carga condicional)
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                await DbInitializer.SeedDataAsync(context);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
