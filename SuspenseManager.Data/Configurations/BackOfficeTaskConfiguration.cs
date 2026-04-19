using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Data.Configurations;

public class BackOfficeTaskConfiguration : IEntityTypeConfiguration<BackOfficeTask>
{
    public void Configure(EntityTypeBuilder<BackOfficeTask> builder)
    {
        builder.ToTable("BackOfficeTasks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProblemDescription)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.TaskStatus).IsRequired();
        builder.Property(x => x.CreateTime).IsRequired();
        builder.Property(x => x.ArchiveLevel).IsRequired().HasDefaultValue(0);

        builder.HasOne(x => x.Group)
            .WithMany(g => g.BackOfficeTasks)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedByAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.GroupId);
        builder.HasIndex(x => x.TaskStatus);
        builder.HasIndex(x => x.ArchiveLevel);
    }
}
