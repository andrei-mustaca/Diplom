using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Diplom.Controllers;

[Authorize(Roles = "Dispatcher")]
public class DispatcherController : Controller
{
    private readonly IDispatcherService _dispatcherService;

    public DispatcherController(IDispatcherService dispatcherService)
    {
        _dispatcherService = dispatcherService;
    }

    // GET: Dispatcher/Index
    public async Task<IActionResult> Index()
    {
        var requests = await _dispatcherService.GetNewRequests();
        return View(requests);
    }

    // POST: Dispatcher/ApproveConsultation
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveConsultation(Guid requestId)
    {
        try
        {
            await _dispatcherService.ApproveConsultation(requestId);
            TempData["SuccessMessage"] = "✅ Консультация одобрена!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"❌ Ошибка: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Dispatcher/RejectRequest
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(Guid requestId, string comment)
    {
        try
        {
            await _dispatcherService.RejectRequest(requestId, comment);
            TempData["SuccessMessage"] = "🚫 Заявка отклонена!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"❌ Ошибка: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Dispatcher/GetWorkers
    [HttpGet]
    public async Task<IActionResult> GetWorkers(Guid requestId)
    {
        var workers = await _dispatcherService.GetAvailableWorkers(requestId);
        ViewBag.RequestId = requestId;
        return PartialView("WorkersList", workers);
    }

    // POST: Dispatcher/AssignWorker
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignWorker(Guid requestId, Guid userId)
    {
        try
        {
            await _dispatcherService.AssignWorker(requestId, userId);
            TempData["SuccessMessage"] = "👷 Работник успешно назначен!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"❌ Ошибка: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
    
    public async Task<IActionResult> Brigades()
    {
            var workers = await _dispatcherService.GetAllWorkersInfo();
            return View(workers);
    }
    
        // GET: Brigades/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var worker = await _dispatcherService.GetWorkerInfo(id);
            if (worker == null)
                return NotFound();
            
            return View(worker);
        }
}