using Domain;
using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;

namespace Services.Service;

public class RequestService:IRequestService
{
    private readonly ApplicationDbContext _context;

    public RequestService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Request> CreateRequest(Request request)
    {
        request.Id = Guid.NewGuid();
        request.RequestDate = DateTime.UtcNow;
        // Приоритет по умолчанию, но не показываем пользователю
        request.Priority = Domain.Enums.Priority.Medium;
        var requestHistory= new HistoryRequest
        {
          IdRequest  =request.Id,
          ChangeDate = request.RequestDate,
          Status = RequestStatus.Submitted,
          Comment="Заявка успешно подана пользователем"
        };
        await _context.RequestHistories.AddAsync(requestHistory);
        await _context.Requests.AddAsync(request);
        await _context.SaveChangesAsync();

        return await _context.Requests
            .Include(r => r.WorkType)
            .FirstOrDefaultAsync(r => r.Id == request.Id);
    }
}