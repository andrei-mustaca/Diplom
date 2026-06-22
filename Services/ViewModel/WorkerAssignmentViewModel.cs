namespace Services.ViewModel;

public class WorkerAssignmentViewModel
{
    public Guid UserId { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public int TodayAssignedCount { get; set; } // Количество назначенных сегодня заявок
    public int TodayAssignedMinutes { get; set; } // Суммарное время назначенных сегодня заявок
    public int TodayCompletedCount { get; set; } // Завершено сегодня
    public int TodayCompletedOnTime { get; set; } // Завершено вовремя
    public int TodayCompletedLate { get; set; } // Завершено с опозданием
    public bool IsAvailable { get; set; } // Доступен ли (менее 8 часов заявок)
    public int RemainingMinutes { get; set; } // Сколько еще минут можно назначить
}