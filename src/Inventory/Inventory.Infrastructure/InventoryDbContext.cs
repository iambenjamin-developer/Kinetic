﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<PendingMessage> PendingMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Id)
                      .ValueGeneratedOnAdd(); // ← CLAVE: marca el ID como autogenerado por la DB

                entity.HasOne(p => p.Category)
                      .WithMany(c => c.Products)
                      .HasForeignKey(p => p.CategoryId);
            });

            // Category configuration (si también es autoincremental)
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Id)
                      .ValueGeneratedOnAdd();
            });

            // PendingMessage configuration
            modelBuilder.Entity<PendingMessage>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Id)
                      .ValueGeneratedOnAdd();

                entity.Property(p => p.EventType)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(p => p.RoutingKey)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(p => p.Payload)
                      .IsRequired();

                entity.Property(p => p.CreatedAt)
                      .IsRequired();

                entity.Property(p => p.RetryCount)
                      .HasDefaultValue(0);

                entity.Property(p => p.IsProcessed)
                      .HasDefaultValue(false);
            });
        }
    }
}
