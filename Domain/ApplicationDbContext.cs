using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Domain;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<WorkType> WorkTypes { get; set; }
    public DbSet<Request> Requests { get; set; }
    public DbSet<RequestAssigment> RequestAssignments { get; set; }
    public DbSet<WorkReport> WorkReports { get; set; }
    public DbSet<HistoryRequest> RequestHistories { get; set; }
    public DbSet<Complaint> Complaints { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}