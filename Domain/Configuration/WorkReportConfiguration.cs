using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configuration;

public class WorkReportConfiguration : IEntityTypeConfiguration<WorkReport>
{
    public void Configure(EntityTypeBuilder<WorkReport> builder)
    {
        builder.HasKey(wr => wr.Id);

        builder.HasOne(wr => wr.Request)
            .WithOne(r => r.WorkReport)           // один-к-одному
            .HasForeignKey<WorkReport>(wr => wr.RequestId);

        builder.HasOne(wr => wr.User)
            .WithMany(u => u.WorkReports)
            .HasForeignKey(wr => wr.UserId);
    }
}