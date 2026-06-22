using Domain.Enums;

namespace Domain.Models;

public class Request
{
    public Guid Id { get; set; }
    public DateTime RequestDate { get; set; }
    public string PhoneClient { get; set; }
    public string EmailClient { get; set; }
    public string DescriptionRequest { get; set; }
    
    public Guid WorkTypeId { get; set; }
    public Priority Priority { get; set; }
    
    public WorkType WorkType { get; set; }
    public List<RequestAssigment> RequestAssigments { get; set; }
    public List<HistoryRequest> HistoryRequests { get; set; }
    public WorkReport WorkReport { get; set; }
}