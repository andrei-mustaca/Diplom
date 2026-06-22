namespace Domain.Models;

public class WorkReport
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid UserId { get; set; }
    public string Description { get; set; }
    public int Duration { get; set; }
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    
    public User User { get; set; }
    public Request Request { get; set; }
}