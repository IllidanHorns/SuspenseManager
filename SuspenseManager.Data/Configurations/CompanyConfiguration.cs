using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LegalName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.ShortName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CompanyCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.BankName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Country).IsRequired().HasMaxLength(10);
        builder.Property(x => x.LegalAddress).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ActualAddress).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Inn).IsRequired().HasMaxLength(12);
        builder.Property(x => x.Bic).IsRequired().HasMaxLength(20);
        builder.Property(x => x.CreateTime).IsRequired();
        builder.Property(x => x.ArchiveLevel).IsRequired();

        builder.HasIndex(x => x.CompanyCode).IsUnique();
        builder.HasIndex(x => x.Inn);
        builder.HasIndex(x => x.ArchiveLevel);
    }
}
