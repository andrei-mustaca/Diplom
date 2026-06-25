using Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using Services.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(120) // 120 секунд
    ));

// Email Service
builder.Services.AddMemoryCache();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<IDispatcherService, DispatcherService>();
builder.Services.AddScoped<IWorkerService, WorkerService>();
builder.Services.AddScoped<IAdminService, AdminService>();

// Регистрация IHttpContextAccessor для работы с HttpContext в сервисах
builder.Services.AddHttpContextAccessor();

// Настройка аутентификации через Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";           // Путь к странице входа
        options.LogoutPath = "/Home/Logout";         // Путь к странице выхода
        options.AccessDeniedPath = "/Home/AccessDenied"; // Путь при отказе в доступе
        options.Cookie.Name = "ExpressGas.Auth";        // Имя cookie
        options.Cookie.HttpOnly = true;                 // Защита от XSS
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Политика безопасности
        options.ExpireTimeSpan = TimeSpan.FromDays(7);  // Время жизни cookie
        options.SlidingExpiration = true;               // Продлевать время при активности
    });

// Добавление авторизации
builder.Services.AddAuthorization(options =>
{
    // Можно добавить политики для разных ролей
    options.AddPolicy("AdministratorOnly", policy => 
        policy.RequireRole("Administrator"));
    options.AddPolicy("DispatcherOnly", policy => 
        policy.RequireRole("Dispatcher"));
    options.AddPolicy("WorkerOnly", policy => 
        policy.RequireRole("Worker"));
    options.AddPolicy("DispatcherOrAdmin", policy => 
        policy.RequireRole("Dispatcher", "Administrator"));
});

// Регистрация ваших сервисов
builder.Services.AddScoped<IWorkTypeService, WorkTypeService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/MainPage");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ВАЖНО: UseAuthentication ДОЛЖЕН быть перед UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=MainPage}/{id?}");

app.Run();