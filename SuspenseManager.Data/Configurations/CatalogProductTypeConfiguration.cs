using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Data.Configurations;

public class CatalogProductTypeConfiguration : IEntityTypeConfiguration<CatalogProductType>
{
    public void Configure(EntityTypeBuilder<CatalogProductType> builder)
    {
        builder.ToTable("CatalogProductTypes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.CreateTime).IsRequired();
        builder.Property(x => x.ArchiveLevel).IsRequired();
    }
}
