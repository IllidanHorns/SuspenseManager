using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Data.Configurations;

public class GroupMetadataConfiguration : IEntityTypeConfiguration<GroupMetadata>
{
    public void Configure(EntityTypeBuilder<GroupMetadata> builder)
    {
        builder.ToTable("GroupMetadata");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SuspenseGroupId).IsRequired();
        builder.Property(x => x.CatalogNumber).HasMaxLength(100);
        builder.Property(x => x.Barcode).HasMaxLength(20);
        builder.Property(x => x.Isrc).HasMaxLength(15);
        builder.Property(x => x.Artist).HasMaxLength(255);
        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.Genre).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.ProductTypeCode).HasMaxLength(50);
        builder.Property(x => x.ProductTypeDesc).HasMaxLength(500);
        builder.Property(x => x.CreateTime).IsRequired();
        builder.Property(x => x.ArchiveLevel).IsRequired();
        builder.Property(x => x.CatalogProductId).IsRequired(false);

        // FK на тип продукта
        builder.HasOne(x => x.ProductType)
            .WithMany()
            .HasForeignKey(x => x.ProductTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK на продукт каталога
        builder.HasOne(x => x.CatalogProduct)
            .WithMany()
            .HasForeignKey(x => x.CatalogProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SuspenseGroupId).IsUnique();
    }
}
