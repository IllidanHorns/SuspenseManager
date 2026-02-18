using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Data.Configurations;

public class SuspenseLineConfiguration : IEntityTypeConfiguration<SuspenseLine>
{
    public void Configure(EntityTypeBuilder<SuspenseLine> builder)
    {
        builder.ToTable("SuspenseLines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Isrc).HasMaxLength(15);
        builder.Property(x => x.Barcode).HasMaxLength(20);
        builder.Property(x => x.CatalogNumber).HasMaxLength(100);
        builder.Property(x => x.SenderCompany).HasMaxLength(255);
        builder.Property(x => x.RecipientCompany).HasMaxLength(255);
        builder.Property(x => x.Operator).HasMaxLength(255);
        builder.Property(x => x.Artist).HasMaxLength(255);
        builder.Property(x => x.TrackTitle).HasMaxLength(255);
        builder.Property(x => x.AgreementType).HasMaxLength(100);
        builder.Property(x => x.AgreementNumber).HasMaxLength(100);
        builder.Property(x => x.TerritoryCode).HasMaxLength(10);
        builder.Property(x => x.CauseSuspense).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Genre).HasMaxLength(100);
        builder.Property(x => x.ExchangeCurrency).HasPrecision(18, 2);
        builder.Property(x => x.ExchangeRate).HasPrecision(18, 6);
        builder.Property(x => x.CreateTime).IsRequired();
        builder.Property(x => x.BusinessStatus).IsRequired();
        builder.Property(x => x.ArchiveLevel).IsRequired();
        builder.Property(x => x.Qty).IsRequired();

        // FK на группу (текущая активная группа)
        builder.HasOne(x => x.Group)
            .WithMany(g => g.SuspenseLines)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        // FK на продукт каталога
        builder.HasOne(x => x.CatalogProduct)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        // FK на компанию отправителя
        builder.HasOne(x => x.SenderCompanyR)
            .WithMany()
            .HasForeignKey(x => x.SenderCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK на компанию получателя
        builder.HasOne(x => x.RecipientCompanyR)
            .WithMany()
            .HasForeignKey(x => x.RecipientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BusinessStatus);
        builder.HasIndex(x => x.Isrc);
        builder.HasIndex(x => x.Barcode);
        builder.HasIndex(x => x.GroupId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.ArchiveLevel);
    }
}
