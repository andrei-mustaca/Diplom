namespace Domain.Models;

public class RequestAssigment
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid UserId { get; set; }
    public DateTime AssigmentDate { get; set; }=DateTime.UtcNow;
    
    public Request Request { get; set; }
    public User User { get; set; }
}