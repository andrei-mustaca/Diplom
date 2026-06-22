using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configuration;

public class RequestAssignmentConfiguration : IEntityTypeConfiguration<RequestAssigment>
{
    public void Configure(EntityTypeBuilder<RequestAssigment> builder)
    {
        builder.HasKey(ra => ra.Id);

        builder.HasOne(ra => ra.Request)
            .WithMany(r => r.RequestAssigments)
            .HasForeignKey(ra => ra.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ra => ra.User)
            .WithMany(u => u.RequestAssigments)
            .HasForeignKey(ra => ra.UserId);
    }
}