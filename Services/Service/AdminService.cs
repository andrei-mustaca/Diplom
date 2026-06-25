using System.Drawing;
using Domain;
using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Services.Interfaces;
using Services.ViewModel;

namespace Services.Service;

public class AdminService:IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly Dictionary<string, string> _pendingConfirmations = new();
    private readonly IMemoryCache _cache;

    public AdminService(ApplicationDbContext context, IEmailService emailService,IMemoryCache cach)
    {
        _cache=cach;
        _context = context;
        _emailService = emailService;
        // Устанавливаем лицензию для EPPlus (NonCommercial)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    // ============= УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯМИ =============

    public async Task<List<UserViewModel>> GetAllUsers()
    {
        return await _context.Users
            .AsNoTracking()
            .Select(u => new UserViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber ?? "-",
                Role = u.Role.ToString()
            })
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<User> GetUserById(Guid userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task DeleteUser(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new Exception("Пользователь не найден");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task<string> SendInvitationEmail(string email, string fullName, string role)
    {
        // Проверяем, не существует ли уже пользователь
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (existingUser != null)
            throw new Exception("Пользователь с таким Email уже существует");

        // Генерируем 6-значный код
        var code = new Random().Next(100000, 999999).ToString();
        
        // Сохраняем код для подтверждения
        _cache.Set($"ConfirmCode_{email}", code, TimeSpan.FromMinutes(10));

        // Отправляем письмо
        var subject = "Приглашение в систему управления бригадами";
        var body = $@"
            <h2>Здравствуйте, {fullName}!</h2>
            <p>Вас пригласили в систему управления выездными бригадами <strong>ЭкспрессГаз</strong>.</p>
            <p>Ваш код подтверждения для завершения регистрации:</p>
            <h1 style='font-size: 32px; color: #2c6b9e; text-align: center; padding: 20px; background: #f0f4f8; border-radius: 8px;'>
                {code}
            </h1>
            <p>Введите этот код в специальном поле на сайте, чтобы завершить регистрацию.</p>
            <p><small>Код действителен в течение 24 часов.</small></p>
        ";

        await _emailService.SendEmailAsync(email, subject, body);
        return code;
    }

    public async Task<bool> ConfirmUserByCode(string email, string code)
    {
        // Получаем код из кеша
        var cacheKey = $"ConfirmCode_{email}";
        var savedCode = _cache.Get<string>(cacheKey);
        
       

        if (string.IsNullOrEmpty(savedCode) || savedCode != code)
        {
            
            return false;
        }

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            _cache.Remove(cacheKey);
            return false;
        }

        var authService = new AuthService(_context, null);
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Новый пользователь",
            Email = email,
            PhoneNumber = "",
            Password = authService.HashPassword("password123"),
            Role = UserRole.Worker
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        _cache.Remove(cacheKey);
        return true;
    }
    

    public async Task UpdateUser(User user)
    {
        var existing = await _context.Users.FindAsync(user.Id);
        if (existing == null)
            throw new Exception("Пользователь не найден");

        existing.FullName = user.FullName;
        existing.PhoneNumber = user.PhoneNumber;
        existing.Role = user.Role;
        
        // Если пароль передан и не пустой, обновляем
        if (!string.IsNullOrEmpty(user.Password))
        {
            var authService = new AuthService(_context, null);
            existing.Password = authService.HashPassword(user.Password);
        }

        await _context.SaveChangesAsync();
    }

    // ============= УПРАВЛЕНИЕ ТИПАМИ РАБОТ =============

    public async Task<List<WorkType>> GetAllWorkTypes()
    {
        return await _context.WorkTypes
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<WorkType> GetWorkTypeById(Guid workTypeId)
    {
        return await _context.WorkTypes.FindAsync(workTypeId);
    }

    public async Task AddWorkType(WorkType workType)
    {
        workType.Id = Guid.NewGuid();
        await _context.WorkTypes.AddAsync(workType);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateWorkType(WorkType workType)
    {
        var existing = await _context.WorkTypes.FindAsync(workType.Id);
        if (existing == null)
            throw new Exception("Тип работы не найден");

        existing.Name = workType.Name;
        existing.Description = workType.Description;
        existing.StandartDuration = workType.StandartDuration;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteWorkType(Guid workTypeId)
    {
        var workType = await _context.WorkTypes.FindAsync(workTypeId);
        if (workType == null)
            throw new Exception("Тип работы не найден");

        // Проверяем, есть ли заявки с этим типом
        var hasRequests = await _context.Requests
            .AnyAsync(r => r.WorkTypeId == workTypeId);

        if (hasRequests)
            throw new Exception("Нельзя удалить тип работы, так как есть заявки, связанные с ним");

        _context.WorkTypes.Remove(workType);
        await _context.SaveChangesAsync();
    }

    // ============= ОТЧЕТЫ =============

    public async Task<List<RequestReportViewModel>> GetAllRequestsReport()
    {
        var requests = await _context.Requests
            .Include(r => r.WorkType)
            .Include(r => r.HistoryRequests)
            .Include(r => r.RequestAssigments)
                .ThenInclude(ra => ra.User)
            .Include(r => r.WorkReport)
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync();

        var result = new List<RequestReportViewModel>();

        foreach (var request in requests)
        {
            var lastHistory = request.HistoryRequests
                .OrderByDescending(h => h.ChangeDate)
                .FirstOrDefault();

            var assignment = request.RequestAssigments?.FirstOrDefault();
            var workerName = assignment?.User?.FullName ?? "Не назначен";

            result.Add(new RequestReportViewModel
            {
                RequestId = request.Id,
                RequestDate = request.RequestDate,
                PhoneClient = request.PhoneClient,
                EmailClient = request.EmailClient,
                DescriptionRequest = request.DescriptionRequest,
                WorkTypeName = request.WorkType?.Name ?? "Не указан",
                Status = lastHistory?.Status.ToString() ?? "Неизвестно",
                WorkerName = workerName,
                Duration = request.WorkReport?.Duration ?? 0
            });
        }

        return result;
    }

    public async Task<byte[]> ExportReportToExcel(DateTime fromDate, DateTime toDate)
    {
        // Получаем все отчеты
        var reports = await GetAllRequestsReport();
        
        // Фильтруем по дате
        var filtered = reports
            .Where(r => r.RequestDate.Date >= fromDate.Date && r.RequestDate.Date <= toDate.Date)
            .OrderByDescending(r => r.RequestDate)
            .ToList();

        using (var package = new ExcelPackage())
        {
            // Создаем лист
            var worksheet = package.Workbook.Worksheets.Add("Отчет по заявкам");

            // ---------- ЗАГОЛОВОК ОТЧЕТА ----------
            worksheet.Cells["A1"].Value = "ОТЧЕТ ПО ЗАЯВКАМ";
            worksheet.Cells["A1"].Style.Font.Size = 18;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.Font.Color.SetColor(Color.DarkBlue);
            worksheet.Row(1).Height = 35;

            // ---------- ИНФОРМАЦИЯ О ПЕРИОДЕ ----------
            worksheet.Cells["A2"].Value = $"Период: с {fromDate:dd.MM.yyyy} по {toDate:dd.MM.yyyy}";
            worksheet.Cells["A2"].Style.Font.Size = 12;
            worksheet.Cells["A2"].Style.Font.Bold = true;
            worksheet.Row(2).Height = 25;

            // ---------- СТАТИСТИКА ----------
            var totalRequests = filtered.Count;
            var completedRequests = filtered.Count(r => r.Status == "Completed");
            var inProgressRequests = filtered.Count(r => r.Status == "InProgress");
            var rejectedRequests = filtered.Count(r => r.Status == "Rejected");
            var submittedRequests = filtered.Count(r => r.Status == "Submitted");

            worksheet.Cells["A4"].Value = "СТАТИСТИКА:";
            worksheet.Cells["A4"].Style.Font.Bold = true;
            worksheet.Cells["A4"].Style.Font.Size = 13;

            worksheet.Cells["A5"].Value = $"Всего заявок: {totalRequests}";
            worksheet.Cells["B5"].Value = $"Выполнено: {completedRequests}";
            worksheet.Cells["C5"].Value = $"В работе: {inProgressRequests}";
            worksheet.Cells["D5"].Value = $"Отклонено: {rejectedRequests}";
            worksheet.Cells["E5"].Value = $"Новые: {submittedRequests}";

            // ---------- ЗАГОЛОВКИ ТАБЛИЦЫ ----------
            var headers = new[]
            {
                "№", "Дата заявки", "Телефон", "Email", "Тип работы", 
                "Описание", "Статус", "Исполнитель", "Время (мин)"
            };

            int headerRow = 7;
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cells[headerRow, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(44, 107, 158));
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }
            worksheet.Row(headerRow).Height = 28;

            // ---------- ЗАПОЛНЕНИЕ ДАННЫХ ----------
            int row = headerRow + 1;
            int index = 1;

            foreach (var report in filtered)
            {
                worksheet.Cells[row, 1].Value = index++;
                worksheet.Cells[row, 2].Value = report.RequestDate.ToString("dd.MM.yyyy HH:mm");
                worksheet.Cells[row, 3].Value = report.PhoneClient;
                worksheet.Cells[row, 4].Value = report.EmailClient;
                worksheet.Cells[row, 5].Value = report.WorkTypeName;
                worksheet.Cells[row, 6].Value = report.DescriptionRequest;
                worksheet.Cells[row, 7].Value = GetStatusText(report.Status);
                worksheet.Cells[row, 8].Value = report.WorkerName;
                worksheet.Cells[row, 9].Value = report.Duration;

                // ---------- ЦВЕТОВАЯ ИНДИКАЦИЯ СТАТУСА ----------
                var statusCell = worksheet.Cells[row, 7];
                switch (report.Status.ToLower())
                {
                    case "completed":
                        statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        statusCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(212, 237, 218));
                        statusCell.Style.Font.Color.SetColor(Color.FromArgb(21, 87, 36));
                        break;
                    case "inprogress":
                        statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        statusCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 243, 205));
                        statusCell.Style.Font.Color.SetColor(Color.FromArgb(133, 100, 4));
                        break;
                    case "submitted":
                        statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        statusCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204, 229, 255));
                        statusCell.Style.Font.Color.SetColor(Color.FromArgb(0, 64, 133));
                        break;
                    case "rejected":
                        statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        statusCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 215, 218));
                        statusCell.Style.Font.Color.SetColor(Color.FromArgb(114, 28, 36));
                        break;
                }

                // ---------- ГРАНИЦЫ ----------
                for (int col = 1; col <= 9; col++)
                {
                    worksheet.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                row++;
            }

            // ---------- АВТОПОДБОР ШИРИНЫ ----------
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Дополнительная настройка ширины для описания
            worksheet.Column(6).Width = 50;

            // ---------- ПОДВАЛ ----------
            int footerRow = row + 2;
            worksheet.Cells[footerRow, 1].Value = "Дата формирования отчета: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            worksheet.Cells[footerRow, 1].Style.Font.Size = 10;
            worksheet.Cells[footerRow, 1].Style.Font.Color.SetColor(Color.Gray);

            // Возвращаем массив байт
            return package.GetAsByteArray();
        }
    }

    // ============= ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =============

    private string GetStatusText(string status)
    {
        return status.ToLower() switch
        {
            "completed" => "✅ Выполнена",
            "inprogress" => "🔄 В работе",
            "submitted" => "📝 Новая",
            "rejected" => "❌ Отклонена",
            _ => status
        };
    }
}