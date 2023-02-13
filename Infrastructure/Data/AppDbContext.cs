using AppCore.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
            if (Database.IsRelational())
            {
                Database.Migrate();
            }
        }

        public DbSet<OrderLog> OrderLogs { get; set; }

        public DbSet<PairsTradingLog> PairsTradingLogs { get; set; }

        public DbSet<RatioCheckLog> RatioCheckLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderLog>().Property(l => l.Price).HasPrecision(18, 8);
            modelBuilder.Entity<OrderLog>().Property(l => l.Amount).HasPrecision(18, 8);
            modelBuilder.Entity<OrderLog>().Property(l => l.Total).HasPrecision(18, 8);

            modelBuilder.Entity<PairsTradingLog>().Property(l => l.LastZ).HasPrecision(18, 8);
            modelBuilder.Entity<PairsTradingLog>().Property(l => l.Ratio).HasPrecision(18, 8);

            modelBuilder.Entity<RatioCheckLog>().Property(l => l.DifferenceInPercents).HasPrecision(18, 8);
            modelBuilder.Entity<RatioCheckLog>().Property(l => l.FirstExchangePrice).HasPrecision(18, 8);
            modelBuilder.Entity<RatioCheckLog>().Property(l => l.SecondExchangePrice).HasPrecision(18, 8);

            base.OnModelCreating(modelBuilder);
        }
    }
}
