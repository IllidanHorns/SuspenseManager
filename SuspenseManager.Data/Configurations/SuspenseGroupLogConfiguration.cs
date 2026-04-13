using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Data.Configurations;

public class SuspenseGroupLogConfiguration : IEntityTypeConfiguration<SuspenseGroupLog>
{
    public void Configure(EntityTypeBuilder<SuspenseGroupLog> builder)
    {
        builder.ToTable("SuspenseGroupLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SuspenseGroupId).IsRequired();
        builder.Property(x => x.StatusTo).IsRequired();
        builder.Property(x => x.AccountId).IsRequired();
        builder.Property(x => x.AccountLogin).IsRequired().HasMaxLength(200);
        builder.Property(x => x.AccountName).HasMaxLength(300);
        builder.Property(x => x.OperationTime).IsRequired();

        builder.HasOne(x => x.SuspenseGroup)
            .WithMany()
            .HasForeignKey(x => x.SuspenseGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SuspenseGroupId);
        builder.HasIndex(x => x.OperationTime);
        builder.HasIndex(x => x.AccountId);
    }
}
