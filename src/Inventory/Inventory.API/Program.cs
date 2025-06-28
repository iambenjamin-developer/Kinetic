using Inventory.API.Policies;
using Inventory.Application;
using Inventory.Application.Interfaces;
using Inventory.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Polly;
using RabbitMQ.Client;
using SharedKernel.Contracts;

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

            // ➕ MassTransit + RabbitMQ
            builder.Services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.UsingRabbitMq((context, cfg) =>
                { /*Nombre que tiene en el docker-compose 'rabbitmq', si probamos en local 'localhost'*/
                    cfg.Host("localhost", "/", h =>
                    {
                        h.Username("rabbitAdmin");
                        h.Password("secretPassword");
                    });

                    const string exchangeName = "inventory_exchange";

                    // Asignar exchange común
                    cfg.Message<ProductCreated>(m => m.SetEntityName(exchangeName));
                    cfg.Publish<ProductCreated>(p =>
                    {
                        p.ExchangeType = ExchangeType.Direct;
                    });

                    cfg.Message<ProductUpdated>(m => m.SetEntityName(exchangeName));
                    cfg.Publish<ProductUpdated>(p =>
                    {
                        p.ExchangeType = ExchangeType.Direct;
                    });

                    cfg.Message<ProductDeleted>(m => m.SetEntityName(exchangeName));
                    cfg.Publish<ProductDeleted>(p =>
                    {
                        p.ExchangeType = ExchangeType.Direct;
                    });

                    // ⚠️ NO usar ConfigureEndpoints ya que esta API no consume mensajes
                });
            });

            //Add Application servicesAdd commentMore actions
            builder.Services.AddApplicationServices(builder.Configuration);

            // Registrar políticas resilientes (Timeout + CircuitBreaker) y adaptador personalizado
            builder.Services.AddSingleton<IResiliencePolicy>(provider =>
            {
                // Timeout: 10 segundos
                var timeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(10));

                // Circuit Breaker: 2 fallos antes de abrir, durante 8 segundos
                var circuitBreaker = Policy
                    .Handle<Exception>()
                    .CircuitBreakerAsync(2, TimeSpan.FromSeconds(8));

                // Unificamos ambas
                var combined = Policy.WrapAsync(circuitBreaker, timeout);
                return new ResiliencePolicy(combined);
            });

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            });


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
