namespace Services.ViewModel;

public class WorkerInfoViewModel
{
    public Guid UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    
    // Статистика за сегодня
    public int TodayAssignedCount { get; set; }
    public int TodayAssignedMinutes { get; set; }
    public int TodayCompletedCount { get; set; }
    public int TodayCompletedOnTime { get; set; }
    public int TodayCompletedLate { get; set; }
    public int TodayInProgressCount { get; set; }
    
    // Статистика за всё время
    public int TotalCompletedCount { get; set; }
    public int TotalRejectedCount { get; set; }
    
    // Текущие заявки в работе
    public List<WorkerCurrentViewModel> CurrentTasks { get; set; }
    
    // Доступность
    public bool IsAvailable { get; set; }
    public int RemainingMinutes { get; set; }
    public double WorkloadPercentage { get; set; } // Процент загрузки (0-100)
}