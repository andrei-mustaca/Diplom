using Domain.Enums;

namespace Services.ViewModel;

public class WorkerTaskViewModel
{
    public Guid AssignmentId { get; set; }
    public Guid RequestId { get; set; }
    public DateTime RequestDate { get; set; }
    public string PhoneClient { get; set; }
    public string EmailClient { get; set; }
    public string DescriptionRequest { get; set; }
    public string WorkTypeName { get; set; }
    public int StandartDuration { get; set; }
    public Priority Priority { get; set; }
    public DateTime AssignedDate { get; set; }
    public string Status { get; set; }
}