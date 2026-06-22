using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configuration;

public class WorkTypeConfiguration : IEntityTypeConfiguration<WorkType>
{
    public void Configure(EntityTypeBuilder<WorkType> builder)
    {
        builder.HasKey(wt => wt.Id);

        builder.Property(wt => wt.Name).IsRequired().HasMaxLength(100);
        builder.Property(wt => wt.Description).HasMaxLength(500);
    }
}