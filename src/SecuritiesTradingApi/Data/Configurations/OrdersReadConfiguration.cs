using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecuritiesTradingApi.Models.Entities;

namespace SecuritiesTradingApi.Data.Configurations;

public class OrdersReadConfiguration : IEntityTypeConfiguration<OrdersRead>
{
    public void Configure(EntityTypeBuilder<OrdersRead> builder)
    {
        builder.ToTable("Orders_Read");
        
        builder.HasKey(e => e.OrderId);
        
        builder.Property(e => e.OrderId)
            .ValueGeneratedNever();
        
        builder.Property(e => e.UserName)
            .HasMaxLength(100);
        
        builder.Property(e => e.StockCode)
            .HasMaxLength(10)
            .IsRequired();
        
        builder.Property(e => e.StockName)
            .HasMaxLength(100);
        
        builder.Property(e => e.StockNameShort)
            .HasMaxLength(50);
        
        builder.Property(e => e.OrderTypeName)
            .HasMaxLength(20);
        
        builder.Property(e => e.Price)
            .HasColumnType("decimal(18,2)");
        
        builder.Property(e => e.FilledQuantity)
            .HasDefaultValue(0);
        
        builder.Property(e => e.OrderStatusName)
            .HasMaxLength(20);
        
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
        
        builder.HasIndex(e => new { e.UserId, e.TradeDate });
        builder.HasIndex(e => e.StockCode);
        builder.HasIndex(e => e.OrderStatus);
    }
}
