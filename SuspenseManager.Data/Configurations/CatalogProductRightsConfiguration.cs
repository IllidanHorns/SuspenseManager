using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Data.Configurations;

public class CatalogProductRightsConfiguration : IEntityTypeConfiguration<CatalogProductRights>
{
    public void Configure(EntityTypeBuilder<CatalogProductRights> builder)
    {
        builder.ToTable("CatalogProductRights");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocNumber).HasMaxLength(100);
        builder.Property(x => x.CompanySender).IsRequired().HasMaxLength(255);
        builder.Property(x => x.CompanyReceiver).IsRequired().HasMaxLength(255);
        builder.Property(x => x.CompanySenderId).IsRequired();
        builder.Property(x => x.CompanyReceiverId).IsRequired();
        builder.Property(x => x.TerritoryCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.TerritoryDesc).IsRequired().HasMaxLength(255);
        builder.Property(x => x.TerritoryId).IsRequired();
        builder.Property(x => x.CatalogProductId).IsRequired();
        builder.Property(x => x.CreateTime).IsRequired();
        builder.Property(x => x.ArchiveLevel).IsRequired();
        
        builder.HasOne(x => x.CatalogProduct)
            .WithMany(p => p.ProductRights)
            .HasForeignKey(x => x.CatalogProductId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(x => x.CompanySenderR)
            .WithMany()
            .HasForeignKey(x => x.CompanySenderId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(x => x.CompanyReceiverR)
            .WithMany()
            .HasForeignKey(x => x.CompanyReceiverId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(x => x.Territory)
            .WithMany()
            .HasForeignKey(x => x.TerritoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CatalogProductId);
        builder.HasIndex(x => x.CompanySenderId);
        builder.HasIndex(x => x.CompanyReceiverId);
        builder.HasIndex(x => x.ArchiveLevel);
    }
}
