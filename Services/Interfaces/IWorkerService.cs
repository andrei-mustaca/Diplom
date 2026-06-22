using Services.ViewModel;

namespace Services.Interfaces;

public interface IWorkerService
{
    Task<List<WorkerTaskViewModel>> GetTodayTasks(Guid userId);
    Task<WorkerTaskDetailViewModel> GetTaskDetail(Guid assignmentId, Guid userId);
    Task CompleteTask(CompleteTaskViewModel model, Guid userId);
}