using System.ComponentModel.DataAnnotations;

namespace Services.ViewModel;

public class AddUserByEmailViewModel
{
    [Required(ErrorMessage = "Введите код подтверждения")]
    [Display(Name = "Код подтверждения")]
    public string ConfirmationCode { get; set; }
    
    public string Email { get; set; }
}