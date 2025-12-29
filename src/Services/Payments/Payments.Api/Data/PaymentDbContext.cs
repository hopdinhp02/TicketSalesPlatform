using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.Payments.Api.Entities;

namespace TicketSalesPlatform.Payments.Api.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
            : base(options) { }

        public DbSet<Payment> Payments => Set<Payment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Payment>().Property(p => p.Version).IsRowVersion();
            modelBuilder.Entity<Payment>().HasIndex(p => p.OrderId).IsUnique();
        }
    }
}
