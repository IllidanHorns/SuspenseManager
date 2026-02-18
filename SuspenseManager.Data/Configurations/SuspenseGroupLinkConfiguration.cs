using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Data.Configurations;

public class SuspenseGroupLinkConfiguration : IEntityTypeConfiguration<SuspenseGroupLink>
{
    public void Configure(EntityTypeBuilder<SuspenseGroupLink> builder)
    {
        builder.ToTable("SuspenseGroupLinks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SuspenseId).IsRequired();
        builder.Property(x => x.SuspenseGroupId).IsRequired();
        builder.Property(x => x.AccountId).IsRequired();
        builder.Property(x => x.CreateTime).IsRequired();
        builder.Property(x => x.BusinessStatus).IsRequired();
        builder.Property(x => x.ArchiveLevel).IsRequired();

        builder.HasOne(x => x.SuspenseLine)
            .WithMany()
            .HasForeignKey(x => x.SuspenseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SuspenseGroup)
            .WithMany(g => g.SuspenseGroupLinks)
            .HasForeignKey(x => x.SuspenseGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.SuspenseId, x.SuspenseGroupId }).IsUnique();
    }
}
