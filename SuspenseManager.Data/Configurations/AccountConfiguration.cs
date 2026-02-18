using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Login).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.CreateTime).IsRequired();
        builder.Property(x => x.ArchiveLevel).IsRequired();

        // 1:1 с User
        builder.HasOne(x => x.User)
            .WithOne(u => u.Account)
            .HasForeignKey<Account>(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // M:M с Rights через AccountRightsLink
        builder.HasMany(x => x.Rights)
            .WithMany(r => r.Accounts)
            .UsingEntity<AccountRightsLink>(
                right => right.HasOne(l => l.Rights)
                    .WithMany(r => r.AccountsLinks)
                    .HasForeignKey(l => l.RightId),
                account => account.HasOne(l => l.Account)
                    .WithMany(a => a.RightsLinks)
                    .HasForeignKey(l => l.AccountId)
            );

        builder.HasIndex(x => x.Login).IsUnique();
    }
}
