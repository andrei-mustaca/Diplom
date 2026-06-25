namespace Services.ViewModel;

public class AdminPanelViewModel
{
    public List<UserViewModel> Users { get; set; } = new();
    public List<WorkTypeViewModel> WorkTypes { get; set; } = new();
    public List<RequestReportViewModel> Reports { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public class UserViewModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Role { get; set; }
}

public class WorkTypeViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int StandartDuration { get; set; }
}

public class RequestReportViewModel
{
    public Guid RequestId { get; set; }
    public DateTime RequestDate { get; set; }
    public string PhoneClient { get; set; }
    public string EmailClient { get; set; }
    public string DescriptionRequest { get; set; }
    public string WorkTypeName { get; set; }
    public string Status { get; set; }
    public string WorkerName { get; set; }
    public int Duration { get; set; }
}