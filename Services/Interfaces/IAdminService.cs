using Domain.Models;
using Services.ViewModel;

namespace Services.Interfaces;

public interface IAdminService
{
    // Управление пользователями
    Task<List<UserViewModel>> GetAllUsers();
    Task<User> GetUserById(Guid userId);
    Task DeleteUser(Guid userId);
    Task<string> SendInvitationEmail(string email, string fullName,string role);
    Task<bool> ConfirmUserByCode(string email, string code);
    Task UpdateUser(User user);
    
    // Управление типами работ
    Task<List<WorkType>> GetAllWorkTypes();
    Task<WorkType> GetWorkTypeById(Guid workTypeId);
    Task AddWorkType(WorkType workType);
    Task UpdateWorkType(WorkType workType);
    Task DeleteWorkType(Guid workTypeId);
    
    // Отчеты
    Task<List<RequestReportViewModel>> GetAllRequestsReport();
    Task<byte[]> ExportReportToExcel(DateTime fromDate, DateTime toDate);
}