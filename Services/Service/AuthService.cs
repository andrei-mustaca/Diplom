using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Domain;
using Domain.Enums;
using Domain.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using AuthenticationProperties = Microsoft.AspNetCore.Http.Authentication.AuthenticationProperties;

namespace Services.Service;

public class AuthService:IAuthService
{
     private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<User> Login(string login, string password, UserRole selectedRole)
    {
        // Ищем пользователя по Email ИЛИ по FullName И с указанной ролью
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                (u.Email == login || u.FullName == login) && 
                u.Role == selectedRole);

        if (user == null)
            throw new Exception("Пользователь с такими данными и выбранной ролью не найден");

        if (!VerifyPassword(password, user.Password))
            throw new Exception("Неверный пароль");

        // Проверяем соответствие роли
        if (user.Role != selectedRole)
            throw new Exception("Выбранная роль не соответствует вашей должности");

        // Создаем claims для аутентификации
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("PhoneNumber", user.PhoneNumber ?? ""),
            new Claim("LoginMethod", login.Contains("@") ? "Email" : "Username")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await _httpContextAccessor.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return user;
    }

    public async Task Logout()
    {
        await AuthenticationHttpContextExtensions.SignOutAsync(_httpContextAccessor.HttpContext);
    }

    public async Task<User> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return null;

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value);
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }

    public UserRole? GetCurrentUserRole()
    {
        var roleClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (Enum.TryParse<UserRole>(roleClaim, out var role))
            return role;
        return null;
    }

    public string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        var hash = HashPassword(password);
        return hash == passwordHash;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }
}