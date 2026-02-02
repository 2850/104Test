using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecuritiesTradingApi.Models.Entities;

namespace SecuritiesTradingApi.Data.Configurations;

public class StockQuotesSnapshotConfiguration : IEntityTypeConfiguration<StockQuotesSnapshot>
{
    public void Configure(EntityTypeBuilder<StockQuotesSnapshot> builder)
    {
        builder.ToTable("StockQuotesSnapshot", t => t.IsMemoryOptimized());
        
        builder.HasKey(e => e.StockCode)
            .IsClustered(false);
        
        builder.Property(e => e.StockCode)
            .HasMaxLength(10)
            .IsRequired();
        
        builder.Property(e => e.CurrentPrice)
            .HasColumnType("decimal(18,2)");
        
        builder.Property(e => e.YesterdayPrice)
            .HasColumnType("decimal(18,2)");
        
        builder.Property(e => e.OpenPrice)
            .HasColumnType("decimal(18,2)");
        
        builder.Property(e => e.HighPrice)
            .HasColumnType("decimal(18,2)");
        
        builder.Property(e => e.LowPrice)
            .HasColumnType("decimal(18,2)");
        
        builder.Property(e => e.LimitUpPrice)
            .HasColumnType("decimal(18,2)");
        
        builder.Property(e => e.LimitDownPrice)
            .HasColumnType("decimal(18,2)");
        
        builder.Property(e => e.ChangeAmount)
            .HasColumnType("decimal(18,2)");
        
        builder.Property(e => e.ChangePercent)
            .HasColumnType("decimal(18,4)");
        
        builder.Property(e => e.TotalValue)
            .HasColumnType("decimal(18,2)");
        
        builder.HasIndex(e => e.UpdateTime)
            .IsClustered(false);
    }
}
