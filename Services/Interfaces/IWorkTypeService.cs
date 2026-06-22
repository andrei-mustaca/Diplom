using Domain.Models;

namespace Services.Interfaces;

public interface IWorkTypeService
{
    Task<List<WorkType>> GetWorkTypes();
}