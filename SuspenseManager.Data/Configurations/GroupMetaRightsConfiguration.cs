using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Data.Configurations;

public class GroupMetaRightsConfiguration : IEntityTypeConfiguration<GroupMetaRights>
{
    public void Configure(EntityTypeBuilder<GroupMetaRights> builder)
    {
        builder.ToTable("GroupMetaRights");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.GroupId).IsRequired();
        builder.Property(x => x.DocNumber).HasMaxLength(100);
        builder.Property(x => x.DocType).HasMaxLength(100);
        builder.Property(x => x.TerritoryDesc).HasMaxLength(255);
        builder.Property(x => x.TerritoryCode).HasMaxLength(10);
        builder.Property(x => x.CreateTime).IsRequired();
        builder.Property(x => x.ArchiveLevel).IsRequired();

        // FK на территорию
        builder.HasOne(x => x.Territory)
            .WithMany()
            .HasForeignKey(x => x.TerritoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK на продукт каталога
        builder.HasOne(x => x.CatalogProduct)
            .WithMany()
            .HasForeignKey(x => x.CatalogProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK на компанию отправителя
        builder.HasOne(x => x.SenderCompany)
            .WithMany()
            .HasForeignKey(x => x.SenderCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK на компанию получателя
        builder.HasOne(x => x.ReceiverCompany)
            .WithMany()
            .HasForeignKey(x => x.ReceiverCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.GroupId).IsUnique();
    }
}
