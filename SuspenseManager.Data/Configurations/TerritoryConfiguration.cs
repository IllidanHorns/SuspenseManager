using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Data.Configurations;

public class TerritoryConfiguration : IEntityTypeConfiguration<Territory>
{
    public void Configure(EntityTypeBuilder<Territory> builder)
    {
        builder.ToTable("Territories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TerritoryCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.TerritoryName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.CreateTime).IsRequired();
        builder.Property(x => x.ArchiveLevel).IsRequired();

        builder.HasIndex(x => x.TerritoryCode).IsUnique();
    }
}
