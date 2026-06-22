using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configuration;

public class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.PhoneClient).IsRequired().HasMaxLength(20);
        builder.Property(r => r.DescriptionRequest).IsRequired().HasMaxLength(1000);

        builder.HasOne(r => r.WorkType)
            .WithMany(wt => wt.Requests)
            .HasForeignKey(r => r.WorkTypeId);
    }
}