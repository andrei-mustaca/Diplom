using Domain;
using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using Services.ViewModel;

namespace Services.Service;

public class WorkerService : IWorkerService
{
    private readonly ApplicationDbContext _context;

    public WorkerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkerTaskViewModel>> GetTodayTasks(Guid userId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var assignments = await _context.RequestAssignments
            .Include(a => a.Request)
                .ThenInclude(r => r.WorkType)
            .Include(a => a.Request.HistoryRequests)
            .Where(a => a.UserId == userId 
                     && a.AssigmentDate >= today 
                     && a.AssigmentDate < tomorrow)
            .OrderByDescending(a => a.AssigmentDate)
            .ToListAsync();

        var tasks = new List<WorkerTaskViewModel>();

        foreach (var assignment in assignments)
        {
            var lastHistory = assignment.Request.HistoryRequests
                .OrderByDescending(h => h.ChangeDate)
                .FirstOrDefault();

            var status = lastHistory?.Status switch
            {
                RequestStatus.Submitted => "Назначена",
                RequestStatus.InProgress => "В работе",
                RequestStatus.Completed => "Завершена",
                RequestStatus.Rejected => "Отклонена",
                _ => "Неизвестно"
            };

            // Показываем только активные задачи
            if (lastHistory?.Status == RequestStatus.InProgress || 
                lastHistory?.Status == RequestStatus.Submitted)
            {
                tasks.Add(new WorkerTaskViewModel
                {
                    AssignmentId = assignment.Id,
                    RequestId = assignment.Request.Id,
                    RequestDate = assignment.Request.RequestDate,
                    PhoneClient = assignment.Request.PhoneClient,
                    EmailClient = assignment.Request.EmailClient,
                    DescriptionRequest = assignment.Request.DescriptionRequest,
                    WorkTypeName = assignment.Request.WorkType?.Name ?? "Не указано",
                    StandartDuration = assignment.Request.WorkType?.StandartDuration ?? 0,
                    Priority = assignment.Request.Priority,
                    AssignedDate = assignment.AssigmentDate,
                    Status = status
                });
            }
        }

        return tasks;
    }

    public async Task<WorkerTaskDetailViewModel> GetTaskDetail(Guid assignmentId, Guid userId)
    {
        var assignment = await _context.RequestAssignments
            .Include(a => a.Request)
                .ThenInclude(r => r.WorkType)
            .Include(a => a.Request.HistoryRequests)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.UserId == userId);

        if (assignment == null)
            throw new Exception("Задача не найдена");

        var lastHistory = assignment.Request.HistoryRequests
            .OrderByDescending(h => h.ChangeDate)
            .FirstOrDefault();

        // Проверяем, есть ли уже отчет
        var existingReport = await _context.WorkReports
            .FirstOrDefaultAsync(w => w.RequestId == assignment.RequestId 
                                   && w.UserId == userId);

        var detail = new WorkerTaskDetailViewModel
        {
            AssignmentId = assignment.Id,
            RequestId = assignment.Request.Id,
            RequestDate = assignment.Request.RequestDate,
            PhoneClient = assignment.Request.PhoneClient,
            EmailClient = assignment.Request.EmailClient,
            DescriptionRequest = assignment.Request.DescriptionRequest,
            WorkTypeName = assignment.Request.WorkType?.Name ?? "Не указано",
            StandartDuration = assignment.Request.WorkType?.StandartDuration ?? 0,
            Priority = assignment.Request.Priority,
            AssignedDate = assignment.AssigmentDate,
            Status = lastHistory?.Status.ToString() ?? "Неизвестно",
            HasReport = existingReport != null,
            Report = existingReport != null ? new WorkReportViewModel
            {
                Description = existingReport.Description,
                Duration = existingReport.Duration,
                ReportedAt = existingReport.ReportedAt
            } : null
        };

        return detail;
    }

    public async Task CompleteTask(CompleteTaskViewModel model, Guid userId)
    {
        // Проверяем, что задача назначена этому пользователю
        var assignment = await _context.RequestAssignments
            .FirstOrDefaultAsync(a => a.Id == model.AssignmentId && a.UserId == userId);

        if (assignment == null)
            throw new Exception("Задача не найдена или не назначена вам");

        // Проверяем, что нет уже отчета
        var existingReport = await _context.WorkReports
            .FirstOrDefaultAsync(w => w.RequestId == model.RequestId && w.UserId == userId);

        if (existingReport != null)
            throw new Exception("Отчет уже был создан ранее");

        // Создаем отчет о работе
        var workReport = new WorkReport
        {
            Id = Guid.NewGuid(),
            RequestId = model.RequestId,
            UserId = userId,
            Description = model.Description,
            Duration = model.Duration,
            ReportedAt = DateTime.UtcNow
        };

        await _context.WorkReports.AddAsync(workReport);

        // Добавляем запись в историю
        var history = new HistoryRequest
        {
            IdRequest = model.RequestId,
            ChangeDate = DateTime.UtcNow,
            Status = RequestStatus.Completed,
            Comment = $"Работа завершена. Затрачено: {model.Duration} мин."
        };

        await _context.RequestHistories.AddAsync(history);
        await _context.SaveChangesAsync();
    }
}