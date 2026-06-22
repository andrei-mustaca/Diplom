using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Services.ViewModel;

namespace Diplom.Controllers;

[Authorize(Roles = "Worker")]
public class WorkerController : Controller
{
    private readonly IWorkerService _workerService;
    private readonly IAuthService _authService;

    public WorkerController(IWorkerService workerService, IAuthService authService)
    {
        _workerService = workerService;
        _authService = authService;
    }

    // GET: Worker/Tasks
    public async Task<IActionResult> Tasks()
    {
        var userId = GetCurrentUserId();
        var tasks = await _workerService.GetTodayTasks(userId);
        return View(tasks);
    }

    // GET: Worker/TaskDetail/5
    public async Task<IActionResult> TaskDetail(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var task = await _workerService.GetTaskDetail(id, userId);
            
            var model = new CompleteTaskViewModel
            {
                RequestId = task.RequestId,
                AssignmentId = task.AssignmentId,
                Duration = task.StandartDuration
            };
            
            ViewBag.TaskDetail = task;
            return View(model);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Tasks));
        }
    }

    // POST: Worker/CompleteTask
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteTask(CompleteTaskViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var userId = GetCurrentUserId();
            var task = await _workerService.GetTaskDetail(model.AssignmentId, userId);
            ViewBag.TaskDetail = task;
            return View("TaskDetail", model);
        }

        try
        {
            var userId = GetCurrentUserId();
            await _workerService.CompleteTask(model, userId);
            TempData["SuccessMessage"] = "✅ Задача успешно завершена!";
            return RedirectToAction(nameof(Tasks));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"❌ Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Tasks));
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim);
    }
}