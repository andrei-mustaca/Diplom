using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Services.ViewModel;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введите логин или email")]
    [Display(Name = "Логин или Email")]
    public string Login { get; set; }

    [Required(ErrorMessage = "Введите пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Выберите роль")]
    [Display(Name = "Роль")]
    public UserRole SelectedRole { get; set; }

    [Display(Name = "Запомнить меня")]
    public bool RememberMe { get; set; }
}