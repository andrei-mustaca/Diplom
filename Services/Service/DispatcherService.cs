using Domain;
using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using Services.ViewModel;

namespace Services.Service;

public class DispatcherService:IDispatcherService
{
    private readonly ApplicationDbContext _context;

    public DispatcherService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<NewRequestViewModel>> GetNewRequests()
    {
        var newStatus = RequestStatus.Submitted;
        
        var requests = await _context.Requests
            .Include(r => r.WorkType)
            .Include(r => r.HistoryRequests)
            .Where(r => r.HistoryRequests
                .OrderByDescending(h => h.ChangeDate)
                .FirstOrDefault().Status == newStatus)
            .OrderByDescending(r => r.RequestDate)
            .Select(r => new NewRequestViewModel
            {
                RequestId = r.Id,
                RequestDate = r.RequestDate,
                PhoneClient = r.PhoneClient,
                EmailClient = r.EmailClient,
                DescriptionRequest = r.DescriptionRequest,
                WorkTypeName = r.WorkType.Name,
                StandartDuration = r.WorkType.StandartDuration,
                Priority = r.Priority,
                IsConsultation = r.WorkType.Name.ToLower().Contains("консультац"),
                CanApprove = r.WorkType.Name.ToLower().Contains("консультац"),
                NeedAssignment = !r.WorkType.Name.ToLower().Contains("консультац")
            })
            .ToListAsync();

        return requests;
    }

    public async Task ApproveConsultation(Guid requestId)
    {
        var request = await _context.Requests
            .Include(r => r.WorkType)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
            throw new Exception("Заявка не найдена");

        if (!request.WorkType.Name.ToLower().Contains("консультац"))
            throw new Exception("Только консультации можно одобрять напрямую");

        // Добавляем запись в историю
        var history = new HistoryRequest
        {
            IdRequest = requestId,
            ChangeDate = DateTime.UtcNow,
            Status = RequestStatus.InProgress,
            Comment = "Консультация одобрена диспетчером"
        };

        await _context.RequestHistories.AddAsync(history);
        await _context.SaveChangesAsync();
    }

    public async Task RejectRequest(Guid requestId, string comment)
    {
        var history = new HistoryRequest
        {
            IdRequest = requestId,
            ChangeDate = DateTime.UtcNow,
            Status = RequestStatus.Rejected,
            Comment = comment ?? "Заявка отклонена диспетчером"
        };

        await _context.RequestHistories.AddAsync(history);
        await _context.SaveChangesAsync();
    }

    public async Task<List<WorkerAssignmentViewModel>> GetAvailableWorkers(Guid requestId)
{
    var today = DateTime.UtcNow.Date;
    var tomorrow = today.AddDays(1);
    
    var request = await _context.Requests
        .Include(r => r.WorkType)
        .FirstOrDefaultAsync(r => r.Id == requestId);

    if (request == null)
        throw new Exception("Заявка не найдена");

    // Получаем всех работников
    var workers = await _context.Users
        .Where(u => u.Role == UserRole.Worker)
        .ToListAsync();

    var workerDtos = new List<WorkerAssignmentViewModel>();

    foreach (var worker in workers)
    {
        // Получаем назначения на сегодня БЕЗ сложных LINQ запросов
        var todayAssignments = await _context.RequestAssignments
            .Where(ra => ra.UserId == worker.Id 
                      && ra.AssigmentDate >= today 
                      && ra.AssigmentDate < tomorrow)
            .ToListAsync();

        var todayAssignedMinutes = 0;
        var todayAssignedCount = todayAssignments.Count;
        var todayCompletedCount = 0;
        var todayCompletedOnTime = 0;
        var todayCompletedLate = 0;

        // Обрабатываем каждое назначение отдельно
        foreach (var assignment in todayAssignments)
        {
            // Загружаем связанные данные
            var requestData = await _context.Requests
                .Include(r => r.WorkType)
                .FirstOrDefaultAsync(r => r.Id == assignment.RequestId);

            if (requestData?.WorkType != null)
            {
                todayAssignedMinutes += requestData.WorkType.StandartDuration;
            }

            // Получаем последнюю запись в истории
            var lastHistory = await _context.RequestHistories
                .Where(h => h.IdRequest == assignment.RequestId)
                .OrderByDescending(h => h.ChangeDate)
                .FirstOrDefaultAsync();

            if (lastHistory != null)
            {
                if (lastHistory.Status == RequestStatus.Completed)
                {
                    todayCompletedCount++;
                    
                    // Проверяем, выполнено ли вовремя
                    if (requestData?.WorkType != null)
                    {
                        var completionTime = (lastHistory.ChangeDate - assignment.AssigmentDate).TotalMinutes;
                        if (completionTime <= requestData.WorkType.StandartDuration)
                            todayCompletedOnTime++;
                        else
                            todayCompletedLate++;
                    }
                }
            }
        }

        var remainingMinutes = 480 - todayAssignedMinutes;

        workerDtos.Add(new WorkerAssignmentViewModel
        {
            UserId = worker.Id,
            FullName = worker.FullName,
            PhoneNumber = worker.PhoneNumber,
            TodayAssignedCount = todayAssignedCount,
            TodayAssignedMinutes = todayAssignedMinutes,
            TodayCompletedCount = todayCompletedCount,
            TodayCompletedOnTime = todayCompletedOnTime,
            TodayCompletedLate = todayCompletedLate,
            RemainingMinutes = remainingMinutes,
            IsAvailable = remainingMinutes >= (request.WorkType?.StandartDuration ?? 0)
        });
    }

    return workerDtos.OrderByDescending(w => w.IsAvailable)
                     .ThenBy(w => w.TodayAssignedMinutes)
                     .ToList();
}

    public async Task AssignWorker(Guid requestId, Guid userId)
    {
        // Проверяем, что заявка существует и не назначена
        var existingAssignment = await _context.RequestAssignments
            .FirstOrDefaultAsync(ra => ra.RequestId == requestId);

        if (existingAssignment != null)
            throw new Exception("Эта заявка уже назначена");

        // Создаем назначение
        var assignment = new RequestAssigment
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            UserId = userId,
            AssigmentDate = DateTime.UtcNow
        };

        await _context.RequestAssignments.AddAsync(assignment);

        // Добавляем запись в историю
        var history = new HistoryRequest
        {
            IdRequest = requestId,
            ChangeDate = DateTime.UtcNow,
            Status = RequestStatus.InProgress,
            Comment = $"Заявка назначена на работника"
        };

        await _context.RequestHistories.AddAsync(history);
        await _context.SaveChangesAsync();
    }
    
    public async Task<List<WorkerInfoViewModel>> GetAllWorkersInfo()
{
    var today = DateTime.UtcNow.Date;
    var tomorrow = today.AddDays(1);
    
    var workers = await _context.Users
        .Where(u => u.Role == UserRole.Worker)
        .ToListAsync();

    var workerDtos = new List<WorkerInfoViewModel>();

    foreach (var worker in workers)
    {
        // Получаем все назначения этого работника
        var allAssignments = await _context.RequestAssignments
            .Where(ra => ra.UserId == worker.Id)
            .ToListAsync();

        // За сегодня
        var todayAssignments = allAssignments
            .Where(a => a.AssigmentDate >= today && a.AssigmentDate < tomorrow)
            .ToList();

        var todayAssignedMinutes = 0;
        var todayAssignedCount = todayAssignments.Count;
        var currentTasks = new List<WorkerCurrentViewModel>();
        var todayCompletedCount = 0;
        var todayCompletedOnTime = 0;
        var todayCompletedLate = 0;
        var todayInProgressCount = 0;
        var totalCompletedCount = 0;
        var totalRejectedCount = 0;

        // Обрабатываем сегодняшние назначения
        foreach (var assignment in todayAssignments)
        {
            var requestData = await _context.Requests
                .Include(r => r.WorkType)
                .FirstOrDefaultAsync(r => r.Id == assignment.RequestId);

            if (requestData?.WorkType != null)
            {
                todayAssignedMinutes += requestData.WorkType.StandartDuration;
            }

            var lastHistory = await _context.RequestHistories
                .Where(h => h.IdRequest == assignment.RequestId)
                .OrderByDescending(h => h.ChangeDate)
                .FirstOrDefaultAsync();

            if (lastHistory != null)
            {
                if (lastHistory.Status == RequestStatus.Completed)
                {
                    todayCompletedCount++;
                    
                    if (requestData?.WorkType != null)
                    {
                        var completionTime = (lastHistory.ChangeDate - assignment.AssigmentDate).TotalMinutes;
                        if (completionTime <= requestData.WorkType.StandartDuration)
                            todayCompletedOnTime++;
                        else
                            todayCompletedLate++;
                    }
                }
                else if (lastHistory.Status == RequestStatus.InProgress)
                {
                    todayInProgressCount++;
                    
                    if (requestData != null)
                    {
                        currentTasks.Add(new WorkerCurrentViewModel
                        {
                            RequestId = requestData.Id,
                            WorkTypeName = requestData.WorkType?.Name ?? "Не указано",
                            Description = requestData.DescriptionRequest,
                            AssignedDate = assignment.AssigmentDate,
                            StandartDuration = requestData.WorkType?.StandartDuration ?? 0,
                            Status = "В работе",
                            Priority = requestData.Priority
                        });
                    }
                }
            }
        }

        // Обрабатываем все назначения для общей статистики
        foreach (var assignment in allAssignments)
        {
            var lastHistory = await _context.RequestHistories
                .Where(h => h.IdRequest == assignment.RequestId)
                .OrderByDescending(h => h.ChangeDate)
                .FirstOrDefaultAsync();

            if (lastHistory != null)
            {
                if (lastHistory.Status == RequestStatus.Completed)
                    totalCompletedCount++;
                else if (lastHistory.Status == RequestStatus.Rejected)
                    totalRejectedCount++;
            }
        }

        var remainingMinutes = 480 - todayAssignedMinutes;

        workerDtos.Add(new WorkerInfoViewModel
        {
            UserId = worker.Id,
            FullName = worker.FullName,
            Email = worker.Email,
            PhoneNumber = worker.PhoneNumber,
            TodayAssignedCount = todayAssignedCount,
            TodayAssignedMinutes = todayAssignedMinutes,
            TodayCompletedCount = todayCompletedCount,
            TodayCompletedOnTime = todayCompletedOnTime,
            TodayCompletedLate = todayCompletedLate,
            TodayInProgressCount = todayInProgressCount,
            TotalCompletedCount = totalCompletedCount,
            TotalRejectedCount = totalRejectedCount,
            CurrentTasks = currentTasks,
            RemainingMinutes = remainingMinutes,
            IsAvailable = remainingMinutes > 0,
            WorkloadPercentage = Math.Min(100, (double)todayAssignedMinutes / 480 * 100)
        });
    }

    return workerDtos.OrderByDescending(w => w.WorkloadPercentage).ToList();
}
    
    public async Task<WorkerInfoViewModel> GetWorkerInfo(Guid userId)
    {
        var workers = await GetAllWorkersInfo();
        return workers.FirstOrDefault(w => w.UserId == userId);
    }
}