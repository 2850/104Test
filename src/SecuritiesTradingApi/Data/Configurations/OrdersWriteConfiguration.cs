using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecuritiesTradingApi.Models.Entities;

namespace SecuritiesTradingApi.Data.Configurations;

public class OrdersWriteConfiguration : IEntityTypeConfiguration<OrdersWrite>
{
    public void Configure(EntityTypeBuilder<OrdersWrite> builder)
    {
        builder.ToTable("Orders_Write");
        
        builder.HasKey(e => e.OrderId);
        
        builder.Property(e => e.OrderId)
            .ValueGeneratedNever();
        
        builder.Property(e => e.StockCode)
            .HasMaxLength(10)
            .UseCollation("Chinese_Taiwan_Stroke_CI_AS")
            .IsRequired();
        
        builder.Property(e => e.Price)
            .HasColumnType("decimal(18,2)");
        
        builder.Property(e => e.OrderStatus)
            .HasDefaultValue((byte)1);
        
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
        
        builder.HasIndex(e => new { e.UserId, e.TradeDate });
        
        builder.HasIndex(e => e.StockCode);
        
        builder.HasOne(e => e.Stock)
            .WithMany(s => s.OrdersWrite)
            .HasForeignKey(e => e.StockCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
