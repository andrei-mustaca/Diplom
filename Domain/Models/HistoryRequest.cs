using Domain.Enums;

namespace Domain.Models;

public class HistoryRequest
{
    public Guid IdRequest { get; set; }
    public DateTime ChangeDate { get; set; }
    
    public RequestStatus Status { get; set; }
    public string? Comment { get; set; }
    
    public Request Request { get; set; }
}