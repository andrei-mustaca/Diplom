using System.ComponentModel.DataAnnotations;

namespace Services.ViewModel;

public class CreateRequestViewModel
{
    [Required(ErrorMessage = "Выберите тип услуги")]
    [Display(Name = "Тип услуги")]
    public Guid WorkTypeId { get; set; }

    [Required(ErrorMessage = "Введите номер телефона")]
    [Phone(ErrorMessage = "Некорректный формат телефона")]
    [Display(Name = "Контактный телефон")]
    public string PhoneClient { get; set; }

    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    [Display(Name = "Email для связи")]
    public string EmailClient { get; set; }

    [Required(ErrorMessage = "Опишите вашу заявку")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Описание должно быть от 10 до 500 символов")]
    [Display(Name = "Описание заявки")]
    public string DescriptionRequest { get; set; }

    // Список доступных услуг для выпадающего списка
    public List<WorkTypeItem> WorkTypes { get; set; } = new();
}
public class WorkTypeItem
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int StandartDuration { get; set; }
}