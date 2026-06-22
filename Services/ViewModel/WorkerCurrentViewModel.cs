using Domain.Enums;

namespace Services.ViewModel;

public class WorkerCurrentViewModel
{
    public Guid RequestId { get; set; }
    public string WorkTypeName { get; set; }
    public string Description { get; set; }
    public DateTime AssignedDate { get; set; }
    public int StandartDuration { get; set; }
    public string Status { get; set; }
    public Priority Priority { get; set; }
}