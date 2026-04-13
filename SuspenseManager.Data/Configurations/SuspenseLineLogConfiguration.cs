using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Data.Configurations;

public class SuspenseLineLogConfiguration : IEntityTypeConfiguration<SuspenseLineLog>
{
    public void Configure(EntityTypeBuilder<SuspenseLineLog> builder)
    {
        builder.ToTable("SuspenseLineLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SuspenseLineId).IsRequired();
        builder.Property(x => x.StatusTo).IsRequired();
        builder.Property(x => x.AccountId).IsRequired();
        builder.Property(x => x.AccountLogin).IsRequired().HasMaxLength(200);
        builder.Property(x => x.AccountName).HasMaxLength(300);
        builder.Property(x => x.OperationTime).IsRequired();

        builder.HasOne(x => x.SuspenseLine)
            .WithMany()
            .HasForeignKey(x => x.SuspenseLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SuspenseLineId);
        builder.HasIndex(x => x.GroupId);
        builder.HasIndex(x => x.OperationTime);
        builder.HasIndex(x => x.AccountId);
    }
}
