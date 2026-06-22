using System.ComponentModel.DataAnnotations;

namespace Services.ViewModel;

public class CompleteTaskViewModel
{
    [Required]
    public Guid RequestId { get; set; }
    
    [Required]
    public Guid AssignmentId { get; set; }
    
    [Required(ErrorMessage = "Опишите выполненную работу")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Описание должно быть от 10 до 1000 символов")]
    [Display(Name = "Описание выполненной работы")]
    public string Description { get; set; }
    
    [Required(ErrorMessage = "Укажите затраченное время")]
    [Range(1, 480, ErrorMessage = "Время должно быть от 1 до 480 минут")]
    [Display(Name = "Затраченное время (минут)")]
    public int Duration { get; set; }
}