using Domain.Enums;

namespace Services.ViewModel;

public class NewRequestViewModel
{
    public Guid RequestId { get; set; }
    public DateTime RequestDate { get; set; }
    public string PhoneClient { get; set; }
    public string EmailClient { get; set; }
    public string DescriptionRequest { get; set; }
    public string WorkTypeName { get; set; }
    public int StandartDuration { get; set; }
    public Priority Priority { get; set; }
    public bool IsConsultation { get; set; } // true - консультация, false - нужен выезд
    
    // Для консультации - сразу можно одобрить
    public bool CanApprove { get; set; }
    
    // Для выезда - нужно распределить
    public bool NeedAssignment { get; set; }
}