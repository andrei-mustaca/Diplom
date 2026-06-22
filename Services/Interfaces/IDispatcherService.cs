using Services.ViewModel;

namespace Services.Interfaces;

public interface IDispatcherService
{
    Task<List<NewRequestViewModel>> GetNewRequests();
    Task ApproveConsultation(Guid requestId);
    Task RejectRequest(Guid requestId, string comment);
    Task<List<WorkerAssignmentViewModel>> GetAvailableWorkers(Guid requestId);
    Task AssignWorker(Guid requestId, Guid userId);
    
    Task<List<WorkerInfoViewModel>> GetAllWorkersInfo();
    Task<WorkerInfoViewModel> GetWorkerInfo(Guid userId);
}