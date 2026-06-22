using Domain.Enums;
using Domain.Models;

namespace Services.Interfaces;

public interface IAuthService
{
    Task<User> Login(string login, string password, UserRole selectedRole);
    Task Logout();
    Task<User> GetCurrentUser();
    bool IsAuthenticated();
    UserRole? GetCurrentUserRole();
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}