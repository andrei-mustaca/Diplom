using Domain;
using Domain.Enums;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Services.Interfaces;
using Services.ViewModel;

namespace Diplom.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IWorkTypeService _workTypeService;
    private readonly IRequestService _requestService;
    private readonly IAuthService _authService;
    public HomeController(ILogger<HomeController> logger,ApplicationDbContext context, IEmailService emailService,IWorkTypeService workTypeService, IRequestService requestService,IAuthService authService)
    {
        _authService = authService;
        _requestService = requestService;
        _workTypeService = workTypeService;
        _context=context;
        _emailService=emailService;
        _logger = logger;
    }

    public IActionResult MainPage()
        {
            if (_authService.IsAuthenticated())
            {
                var role = _authService.GetCurrentUserRole();
                
                return role switch
                {
                    UserRole.Administrator => RedirectToAction("AdminDashboard"),
                    UserRole.Dispatcher => RedirectToAction("DispatcherDashboard"),
                    UserRole.Worker => RedirectToAction("WorkerDashboard"),
                    _ => View()
                };
            }
            
            return View();
        }
    
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> AdminDashboard()
        {
            var user = await _authService.GetCurrentUser();
            ViewBag.UserName = user?.FullName;
            ViewBag.UserRole = "Администратор";
            return View();
        }
    
        [Authorize(Roles = "Dispatcher")]
        public async Task<IActionResult> DispatcherDashboard()
        {
            var user = await _authService.GetCurrentUser();
            ViewBag.UserName = user?.FullName;
            ViewBag.UserRole = "Диспетчер";
            return View();
        }
    
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> WorkerDashboard()
        {
            var user = await _authService.GetCurrentUser();
            ViewBag.UserName = user?.FullName;
            ViewBag.UserRole = "Рабочий";
            return View();
        }

    public IActionResult AboutPage()
    {
        return View();
    }
    
    [HttpGet]
    public IActionResult Contacts()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contacts(Complaint model)
    {
        if (!ModelState.IsValid)
            return View(model);
    
        model.Id = Guid.NewGuid();
        model.CreatedAt = DateTime.UtcNow;
    
        _context.Complaints.Add(model);
        await _context.SaveChangesAsync();
    
        string subject = "Новое письмо с сайта ЭкспрессГаз";
        string body = $"<h2>Обратная связь</h2>" +
            $"<p><strong>Имя:</strong> {model.Name}</p>" +
            $"<p><strong>Телефон:</strong> {model.PhoneNumber}</p>" +
            $"<p><strong>Email:</strong> {model.Email}</p>" +
            $"<p><strong>Описание:</strong> {model.Description}</p>" +
            $"<p><strong>Дата:</strong> {model.CreatedAt:dd.MM.yyyy HH:mm}</p>";
    
        await _emailService.SendEmailAsync("andrejmustaca6@gmail.com", subject, body);
    
        TempData["SuccessMessage"] = "Ваше обращение отправлено! Мы свяжемся с вами в ближайшее время.";
        return RedirectToAction("Contacts");
    }
    
    public async Task<IActionResult> Works()
    {
        var workTypes = await _workTypeService.GetWorkTypes();
        return View(workTypes);
    }
    
// GET: Requests/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var workTypes = await _workTypeService.GetWorkTypes();
        
        var model = new CreateRequestViewModel
        {
            WorkTypes = workTypes.Select(w => new WorkTypeItem
            {
                Id = w.Id,
                Name = w.Name,
                Description = w.Description,
                StandartDuration = w.StandartDuration
            }).ToList()
        };

        return View(model);
    }

    // POST: Requests/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Перезагружаем список услуг при ошибке валидации
            var workTypes = await _workTypeService.GetWorkTypes();
            model.WorkTypes = workTypes.Select(w => new WorkTypeItem
            {
                Id = w.Id,
                Name = w.Name,
                Description = w.Description,
                StandartDuration = w.StandartDuration
            }).ToList();
            
            return View(model);
        }

        var request = new Request
        {
            WorkTypeId = model.WorkTypeId,
            PhoneClient = model.PhoneClient,
            EmailClient = model.EmailClient,
            DescriptionRequest = model.DescriptionRequest
        };

        await _requestService.CreateRequest(request);

        TempData["SuccessMessage"] = "✅ Ваша заявка успешно отправлена! Мы свяжемся с вами в ближайшее время.";
        return RedirectToAction(nameof(Success));
    }

    // GET: Requests/Success
    public IActionResult Success()
    {
        if (TempData["SuccessMessage"] == null)
            return RedirectToAction(nameof(Create));
            
        ViewBag.Message = TempData["SuccessMessage"];
        return View();
    }
    
    // GET: Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (_authService.IsAuthenticated())
            {
                return RedirectToAction("MainPage", "Home");
            }
    
            ViewBag.ReturnUrl = returnUrl;
            
            var model = new LoginViewModel();
            ViewBag.Roles = GetRolesList();
            
            return View(model);
        }
    
        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = GetRolesList();
                return View(model);
            }
    
            try
            {
                var user = await _authService.Login(model.Login, model.Password, model.SelectedRole);
                
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                    
                return RedirectToAction("MainPage", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Roles = GetRolesList();
                return View(model);
            }
        }
    
        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _authService.Logout();
            return RedirectToAction("MainPage", "Home");
        }
    
        // GET: Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    
        private List<SelectListItem> GetRolesList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = UserRole.Administrator.ToString(), Text = "👨‍💼 Администратор" },
                new SelectListItem { Value = UserRole.Dispatcher.ToString(), Text = "📋 Диспетчер" },
                new SelectListItem { Value = UserRole.Worker.ToString(), Text = "🔧 Рабочий" }
            };
        }
        
        public IActionResult WorkerEntry()
        {
            return View();
        }
}