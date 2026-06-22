using Domain.Models;

namespace Services.Interfaces;

public interface IRequestService
{
    Task<Request> CreateRequest(Request request);
    
}