using Microsoft.EntityFrameworkCore;
using SecuritiesTradingApi.Models.Entities;

namespace SecuritiesTradingApi.Data;

public class TradingDbContext : DbContext
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options)
    {
    }

    public DbSet<StockMaster> StockMaster { get; set; } = null!;
    public DbSet<StockQuotesSnapshot> StockQuotesSnapshot { get; set; } = null!;
    public DbSet<OrdersWrite> OrdersWrite { get; set; } = null!;
    public DbSet<OrdersRead> OrdersRead { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all configurations from the same assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingDbContext).Assembly);
    }
}
