using Domain;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;

namespace Services.Service;

public class WorkTypeService:IWorkTypeService
{
    private readonly ApplicationDbContext _context;

    public WorkTypeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkType>> GetWorkTypes()
    {
        return await _context.WorkTypes
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .ToListAsync();
    }
    
}