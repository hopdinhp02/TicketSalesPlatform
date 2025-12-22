using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.Inventory.Api.Entities;

namespace TicketSalesPlatform.Inventory.Api.Data
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
            : base(options) { }

        public DbSet<Seat> Seats => Set<Seat>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Seat>().Property(s => s.Version).IsRowVersion();
        }
    }
}
