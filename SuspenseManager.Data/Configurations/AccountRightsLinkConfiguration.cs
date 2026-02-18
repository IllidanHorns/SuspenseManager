using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Data.Configurations;

public class AccountRightsLinkConfiguration : IEntityTypeConfiguration<AccountRightsLink>
{
    public void Configure(EntityTypeBuilder<AccountRightsLink> builder)
    {
        builder.ToTable("AccountRightsLinks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RightId).IsRequired();
        builder.Property(x => x.AccountId).IsRequired();
        builder.Property(x => x.CreateTime).IsRequired();
        builder.Property(x => x.ArchiveLevel).IsRequired();

        builder.HasIndex(x => new { x.AccountId, x.RightId }).IsUnique();
    }
}
