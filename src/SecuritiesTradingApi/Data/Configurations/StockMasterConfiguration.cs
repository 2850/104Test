using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecuritiesTradingApi.Models.Entities;

namespace SecuritiesTradingApi.Data.Configurations;

public class StockMasterConfiguration : IEntityTypeConfiguration<StockMaster>
{
    public void Configure(EntityTypeBuilder<StockMaster> builder)
    {
        builder.ToTable("StockMaster");
        
        builder.HasKey(e => e.StockCode);
        
        builder.Property(e => e.StockCode)
            .HasMaxLength(10)
            .UseCollation("Chinese_Taiwan_Stroke_CI_AS")
            .IsRequired();
        
        builder.Property(e => e.StockName)
            .HasMaxLength(100)
            .UseCollation("Chinese_Taiwan_Stroke_CI_AS")
            .IsRequired();
        
        builder.Property(e => e.StockNameShort)
            .HasMaxLength(50)
            .UseCollation("Chinese_Taiwan_Stroke_CI_AS")
            .IsRequired();
        
        builder.Property(e => e.StockNameEn)
            .HasMaxLength(200)
            .UseCollation("Chinese_Taiwan_Stroke_CI_AS");
        
        builder.Property(e => e.Exchange)
            .HasMaxLength(10)
            .UseCollation("Chinese_Taiwan_Stroke_CI_AS")
            .IsRequired();
        
        builder.Property(e => e.Industry)
            .HasMaxLength(50)
            .UseCollation("Chinese_Taiwan_Stroke_CI_AS");
        
        builder.Property(e => e.LotSize)
            .HasDefaultValue(1000);
        
        builder.Property(e => e.AllowOddLot)
            .HasDefaultValue(false);
        
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);
        
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
        
        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
        
        builder.HasIndex(e => e.Exchange);
        builder.HasIndex(e => e.IsActive);
    }
}
