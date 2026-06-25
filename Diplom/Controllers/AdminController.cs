using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // ← ДОБАВИТЬ
using Services.Interfaces;
using Services.ViewModel;

namespace Diplom.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger; // ← ДОБАВИТЬ

    public AdminController(IAdminService adminService, ILogger<AdminController> logger) // ← ДОБАВИТЬ
    {
        _adminService = adminService;
        _logger = logger; // ← ДОБАВИТЬ
    }

    // ============= ГЛАВНАЯ =============
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("=== ЗАГРУЗКА ГЛАВНОЙ СТРАНИЦЫ АДМИНКИ ===");
        
        try
        {
            var users = await _adminService.GetAllUsers();
            var workTypes = await _adminService.GetAllWorkTypes();
            var reports = await _adminService.GetAllRequestsReport();

            _logger.LogInformation("Загружено пользователей: {UserCount}, типов работ: {WorkTypeCount}, отчетов: {ReportCount}", 
                users.Count, workTypes.Count, reports.Count);

            var model = new AdminPanelViewModel
            {
                Users = users,
                WorkTypes = workTypes.Select(w => new WorkTypeViewModel
                {
                    Id = w.Id,
                    Name = w.Name,
                    Description = w.Description,
                    StandartDuration = w.StandartDuration
                }).ToList(),
                Reports = reports
            };

            if (TempData["SuccessMessage"] != null)
            {
                model.SuccessMessage = TempData["SuccessMessage"].ToString();
                _logger.LogInformation("SuccessMessage: {Message}", model.SuccessMessage);
            }
            if (TempData["ErrorMessage"] != null)
            {
                model.ErrorMessage = TempData["ErrorMessage"].ToString();
                _logger.LogWarning("ErrorMessage: {Message}", model.ErrorMessage);
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке главной страницы админки");
            throw;
        }
    }

    // ============= ПОЛЬЗОВАТЕЛИ =============

    // Страница добавления пользователя
    [HttpGet]
    public IActionResult AddUser()
    {
        _logger.LogInformation("=== ОТКРЫТА СТРАНИЦА ДОБАВЛЕНИЯ ПОЛЬЗОВАТЕЛЯ ===");
        return View();
    }

    // Отправка приглашения
    [HttpPost]
    public async Task<IActionResult> AddUser(string email, string fullName, string role)
    {
        _logger.LogInformation("=== ПОПЫТКА ДОБАВЛЕНИЯ ПОЛЬЗОВАТЕЛЯ ===");
        _logger.LogInformation("Email: {Email}, ФИО: {FullName}, Роль: {Role}", email, fullName, role);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(fullName))
        {
            _logger.LogWarning("Email или ФИО пустые");
            TempData["ErrorMessage"] = "Email и ФИО обязательны";
            return RedirectToAction(nameof(AddUser));
        }

        try
        {
            var code = await _adminService.SendInvitationEmail(email, fullName, role);
            _logger.LogInformation("Код подтверждения для {Email}: {Code}", email, code);
            TempData["SuccessMessage"] = $"Приглашение отправлено на {email}. Код: {code}";
            return RedirectToAction(nameof(ConfirmUser), new { email });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке приглашения для {Email}", email);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(AddUser));
        }
    }

    // Страница подтверждения
    [HttpGet]
    public IActionResult ConfirmUser(string email)
    {
        _logger.LogInformation("=== ОТКРЫТА СТРАНИЦА ПОДТВЕРЖДЕНИЯ ПОЛЬЗОВАТЕЛЯ ===");
        _logger.LogInformation("Email для подтверждения: {Email}", email);

        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Email не указан");
            TempData["ErrorMessage"] = "Email не указан";
            return RedirectToAction(nameof(Index));
        }
        return View(new AddUserByEmailViewModel { Email = email });
    }

    // Подтверждение кода
    [HttpPost]
    public async Task<IActionResult> ConfirmUser(AddUserByEmailViewModel model)
    {
        _logger.LogInformation("=== ПОПЫТКА ПОДТВЕРЖДЕНИЯ ПОЛЬЗОВАТЕЛЯ ===");
        _logger.LogInformation("Email: {Email}, Введенный код: {Code}", model.Email, model.ConfirmationCode);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState невалиден для {Email}", model.Email);
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("Ошибка валидации: {Error}", error.ErrorMessage);
            }
            return View(model);
        }

        try
        {
            var isConfirmed = await _adminService.ConfirmUserByCode(model.Email, model.ConfirmationCode);
            _logger.LogInformation("Результат подтверждения для {Email}: {IsConfirmed}", model.Email, isConfirmed);

            if (isConfirmed)
            {
                TempData["SuccessMessage"] = "✅ Пользователь успешно добавлен!";
                _logger.LogInformation("✅ Пользователь {Email} успешно подтвержден", model.Email);
                return RedirectToAction(nameof(Index));
            }

            _logger.LogWarning("❌ Неверный код для {Email}", model.Email);
            TempData["ErrorMessage"] = "❌ Неверный код подтверждения. Попробуйте еще раз.";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при подтверждении пользователя {Email}", model.Email);
            TempData["ErrorMessage"] = $"Ошибка: {ex.Message}";
            return View(model);
        }
    }

    // Редактирование пользователя
    [HttpGet]
    public async Task<IActionResult> EditUser(Guid id)
    {
        _logger.LogInformation("=== ОТКРЫТА СТРАНИЦА РЕДАКТИРОВАНИЯ ПОЛЬЗОВАТЕЛЯ ===");
        _logger.LogInformation("ID пользователя: {Id}", id);
        ModelState.Remove("RequestAssigments");
        ModelState.Remove("WorkReports");
        try
        {
            var user = await _adminService.GetUserById(id);
            if (user == null)
            {
                _logger.LogWarning("Пользователь с ID {Id} не найден", id);
                TempData["ErrorMessage"] = "Пользователь не найден";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("Найден пользователь: {FullName}, Email: {Email}", user.FullName, user.Email);
            return View(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке пользователя {Id}", id);
            TempData["ErrorMessage"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> EditUser(User user)
    {
        ModelState.Remove("RequestAssigments");
        ModelState.Remove("WorkReports");
        _logger.LogInformation("=== ПОПЫТКА РЕДАКТИРОВАНИЯ ПОЛЬЗОВАТЕЛЯ ===");
        _logger.LogInformation("ID: {Id}, ФИО: {FullName}, Email: {Email}, Роль: {Role}", 
            user.Id, user.FullName, user.Email, user.Role);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState невалиден для пользователя {Id}", user.Id);
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("Ошибка валидации: {Error}", error.ErrorMessage);
            }
            return View(user);
        }

        try
        {
            await _adminService.UpdateUser(user);
            _logger.LogInformation("✅ Пользователь {Id} успешно обновлен", user.Id);
            TempData["SuccessMessage"] = "✅ Пользователь обновлен";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении пользователя {Id}", user.Id);
            TempData["ErrorMessage"] = ex.Message;
            return View(user);
        }
    }

    // Удаление пользователя
    [HttpPost]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        _logger.LogInformation("=== ПОПЫТКА УДАЛЕНИЯ ПОЛЬЗОВАТЕЛЯ ===");
        _logger.LogInformation("ID пользователя: {Id}", id);

        try
        {
            await _adminService.DeleteUser(id);
            _logger.LogInformation("✅ Пользователь {Id} удален", id);
            TempData["SuccessMessage"] = "✅ Пользователь удален";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении пользователя {Id}", id);
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    // ============= ТИПЫ РАБОТ =============

    // Страница добавления типа работы
    [HttpGet]
    public IActionResult AddWorkType()
    {
        _logger.LogInformation("=== ОТКРЫТА СТРАНИЦА ДОБАВЛЕНИЯ ТИПА РАБОТЫ ===");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddWorkType(WorkType workType)
    {
        _logger.LogInformation("=== ПОПЫТКА ДОБАВЛЕНИЯ ТИПА РАБОТЫ ===");
        _logger.LogInformation("Название: {Name}, Описание: {Description}, Время: {Duration}", 
            workType.Name, workType.Description, workType.StandartDuration);
        ModelState.Remove("Requests");
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState невалиден для типа работы");
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("Ошибка валидации: {Error}", error.ErrorMessage);
            }
            return View(workType);
        }

        try
        {
            await _adminService.AddWorkType(workType);
            _logger.LogInformation("✅ Тип работы '{Name}' успешно добавлен", workType.Name);
            TempData["SuccessMessage"] = "✅ Тип работы добавлен";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении типа работы '{Name}'", workType.Name);
            TempData["ErrorMessage"] = ex.Message;
            return View(workType);
        }
    }

    // Редактирование типа работы
    [HttpGet]
    public async Task<IActionResult> EditWorkType(Guid id)
    {
        _logger.LogInformation("=== ОТКРЫТА СТРАНИЦА РЕДАКТИРОВАНИЯ ТИПА РАБОТЫ ===");
        _logger.LogInformation("ID типа работы: {Id}", id);

        try
        {
            var workType = await _adminService.GetWorkTypeById(id);
            if (workType == null)
            {
                _logger.LogWarning("Тип работы с ID {Id} не найден", id);
                TempData["ErrorMessage"] = "Тип работы не найден";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("Найден тип работы: {Name}", workType.Name);
            return View(workType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке типа работы {Id}", id);
            TempData["ErrorMessage"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> EditWorkType(WorkType workType)
    {
        _logger.LogInformation("=== ПОПЫТКА РЕДАКТИРОВАНИЯ ТИПА РАБОТЫ ===");
        _logger.LogInformation("ID: {Id}, Название: {Name}, Описание: {Description}, Время: {Duration}", 
            workType.Id, workType.Name, workType.Description, workType.StandartDuration);
        ModelState.Remove("Requests");
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState невалиден для типа работы {Id}", workType.Id);
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("Ошибка валидации: {Error}", error.ErrorMessage);
            }
            return View(workType);
        }

        try
        {
            await _adminService.UpdateWorkType(workType);
            _logger.LogInformation("✅ Тип работы {Id} успешно обновлен", workType.Id);
            TempData["SuccessMessage"] = "✅ Тип работы обновлен";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении типа работы {Id}", workType.Id);
            TempData["ErrorMessage"] = ex.Message;
            return View(workType);
        }
    }

    // Удаление типа работы
    [HttpPost]
    public async Task<IActionResult> DeleteWorkType(Guid id)
    {
        _logger.LogInformation("=== ПОПЫТКА УДАЛЕНИЯ ТИПА РАБОТЫ ===");
        _logger.LogInformation("ID типа работы: {Id}", id);

        try
        {
            await _adminService.DeleteWorkType(id);
            _logger.LogInformation("✅ Тип работы {Id} удален", id);
            TempData["SuccessMessage"] = "✅ Тип работы удален";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении типа работы {Id}", id);
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    // ============= ЭКСПОРТ =============
    [HttpPost]
    public async Task<IActionResult> ExportReport(DateTime fromDate, DateTime toDate)
    {
        _logger.LogInformation("=== ЭКСПОРТ ОТЧЕТА ===");
        _logger.LogInformation("Период: с {FromDate} по {ToDate}", fromDate, toDate);

        try
        {
            var fileBytes = await _adminService.ExportReportToExcel(fromDate, toDate);
            _logger.LogInformation("✅ Отчет успешно экспортирован. Размер: {Size} байт", fileBytes.Length);
            
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Отчет_{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при экспорте отчета за период {FromDate}-{ToDate}", fromDate, toDate);
            TempData["ErrorMessage"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }
}