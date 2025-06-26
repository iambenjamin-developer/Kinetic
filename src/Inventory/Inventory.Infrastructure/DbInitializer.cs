using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure
{
    public static class DbInitializer
    {
        public static async Task SeedDataAsync(InventoryDbContext context)
        {
            // Migraciones pendientes
            await context.Database.MigrateAsync();

            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Electronics" },
                    new Category { Name = "Books" },
                    new Category { Name = "Groceries" }
                };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            if (!context.Products.Any())
            {
                var categories = await context.Categories.ToListAsync();

                long electronicIdCategory = categories.FirstOrDefault(c => c.Name == "Electronics")?.Id ?? 0;
                long bookIdCategory = categories.FirstOrDefault(c => c.Name == "Books")?.Id ?? 0;
                long groceriesIdCategory = categories.FirstOrDefault(c => c.Name == "Groceries")?.Id ?? 0;

                var products = new List<Product>
                {
                    new Product { Name = "Wireless Mouse", Description = "Ergonomic wireless mouse with USB receiver", Price = 29.99m, Stock = 50, CategoryId = electronicIdCategory },
                    new Product { Name = "Bluetooth Headphones", Description = "Noise-cancelling over-ear headphones", Price = 89.99m, Stock = 25, CategoryId = electronicIdCategory },
                    new Product { Name = "The Clean Coder", Description = "A Code of Conduct for Professional Programmers", Price = 39.50m, Stock = 15, CategoryId = bookIdCategory},
                    new Product { Name = "Olive Oil 1L", Description = "Extra virgin olive oil, cold pressed", Price = 10.25m, Stock = 80, CategoryId = groceriesIdCategory },
                    new Product { Name = "Pasta Spaghetti 500g", Description = "Durum wheat pasta", Price = 2.30m, Stock = 200, CategoryId = groceriesIdCategory }
                };

                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }
        }
    }
}
