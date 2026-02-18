using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Data.Configurations;

public class CatalogProductConfiguration : IEntityTypeConfiguration<CatalogProduct>
{
    public void Configure(EntityTypeBuilder<CatalogProduct> builder)
    {
        builder.ToTable("CatalogProducts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductFormatCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ProductTypeDesc).HasMaxLength(500);
        builder.Property(x => x.ProductName).HasMaxLength(255);
        builder.Property(x => x.Barcode).IsRequired().HasMaxLength(20);
        builder.Property(x => x.AlbumName).HasMaxLength(255);
        builder.Property(x => x.Artist).HasMaxLength(255);
        builder.Property(x => x.CatalogNumber).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Composer).HasMaxLength(255);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Isrc).IsRequired().HasMaxLength(15);
        builder.Property(x => x.Genre).HasMaxLength(100);
        builder.Property(x => x.ProductTypeId).IsRequired();
        builder.Property(x => x.CreateTime).IsRequired();
        builder.Property(x => x.ArchiveLevel).IsRequired();

        // FK на тип продукта
        builder.HasOne(x => x.ProductType)
            .WithMany(t => t.CatalogProducts)
            .HasForeignKey(x => x.ProductTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Isrc);
        builder.HasIndex(x => x.Barcode);
        builder.HasIndex(x => x.CatalogNumber);
        builder.HasIndex(x => new { x.ProductName, x.Artist });
        builder.HasIndex(x => x.ArchiveLevel);
    }
}
