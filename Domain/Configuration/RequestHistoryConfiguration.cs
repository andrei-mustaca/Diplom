using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configuration;

public class RequestHistoryConfiguration : IEntityTypeConfiguration<HistoryRequest>
{
    public void Configure(EntityTypeBuilder<HistoryRequest> builder)
    {
        // Составной первичный ключ
        builder.HasKey(h => new { h.IdRequest, h.ChangeDate });

        builder.Property(h => h.ChangeDate)
            .IsRequired();

        builder.Property(h => h.Status)
            .IsRequired();

        builder.Property(h => h.Comment)
            .HasMaxLength(500);

        // Настройка связи
        builder.HasOne(h => h.Request)
            .WithMany(r => r.HistoryRequests)
            .HasForeignKey(h => h.IdRequest)
            .OnDelete(DeleteBehavior.Cascade);
    }
}