using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Data.Configurations;

public class SuspenseGroupConfiguration : IEntityTypeConfiguration<SuspenseGroup>
{
    public void Configure(EntityTypeBuilder<SuspenseGroup> builder)
    {
        builder.ToTable("SuspenseGroups");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessStatus).IsRequired();
        builder.Property(x => x.CreateTime).IsRequired();
        builder.Property(x => x.ArchiveLevel).IsRequired();
        builder.Property(x => x.AccountId).IsRequired();

        // FK на аккаунт (кто создал группу)
        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK на продукт каталога
        builder.HasOne(x => x.CatalogProduct)
            .WithMany()
            .HasForeignKey(x => x.CatalogProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1:1 с GroupMetadata
        builder.HasOne(x => x.GroupMetaData)
            .WithOne(m => m.SuspenseGroup)
            .HasForeignKey<GroupMetadata>(m => m.SuspenseGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1:1 с GroupMetaRights
        builder.HasOne(x => x.GroupMetaRights)
            .WithOne(m => m.SuspenseGroup)
            .HasForeignKey<GroupMetaRights>(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.BusinessStatus);
        builder.HasIndex(x => x.AccountId);
        builder.HasIndex(x => x.CatalogProductId);
        builder.HasIndex(x => x.ArchiveLevel);
    }
}
