using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPanel.Constants;
using SupportPanel.Interfaces;
using SupportPanel.Models;
using SupportPanel.Services;
using SupportPanel.ViewModels;

namespace SupportPanel.Controllers
{
    [Authorize]
    public class TicketController : Controller
    {
        private readonly ITicketService _ticketService;
        private readonly ITenantService _tenantService;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IUserService _userService;
        private readonly ITicketCommentService _ticketCommentService;
        private readonly ITicketAttachmentService _ticketAttachmentService;
        private readonly ITicketHistoryService _ticketHistoryService;
        private readonly ISlaLevelService _slaLevelService;
        private readonly INotificationService _notificationService;
        private readonly IUserProductService _userProductService;
        private readonly ITenantProductService _tenantProductService;
        private readonly ISlaPauseService _slaPauseService;

        private int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private int CurrentTenantId => int.TryParse(User.FindFirstValue("TenantId"), out var id) ? id : 0;

        public TicketController(
            ITicketService ticketService,
            ITenantService tenantService,
            IProductService productService,
            ICategoryService categoryService,
            IUserService userService,
            ITicketCommentService ticketCommentService,
            ITicketHistoryService ticketHistoryService,
            ITicketAttachmentService ticketAttachmentService,
            ISlaLevelService slaLevelService,
            INotificationService notificationService,
            IUserProductService userProductService,
            ITenantProductService tenantProductService,
            ISlaPauseService slaPauseService)
        {
            _ticketService = ticketService;
            _tenantService = tenantService;
            _productService = productService;
            _categoryService = categoryService;
            _userService = userService;
            _ticketCommentService = ticketCommentService;
            _ticketHistoryService = ticketHistoryService;
            _ticketAttachmentService = ticketAttachmentService;
            _slaLevelService = slaLevelService;
            _notificationService = notificationService;
            _userProductService = userProductService;
            _tenantProductService = tenantProductService;
            _slaPauseService = slaPauseService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole(RoleNames.CompanyUser) ||
                User.IsInRole(RoleNames.CompanyManager))
            {
                ViewData["Title"] = "Taleplerim";
                var ownTickets = (await _ticketService.GetAllAsync(CurrentTenantId))
                    .Where(t => t.CreatedByUserId == CurrentUserId)
                    .ToList();
                await PrepareTicketSlaListAsync(ownTickets);
                return View(ownTickets);
            }

            if (User.IsInRole(RoleNames.SupportSpecialist))
            {
                var productIds = (await _userProductService.GetAllAsync())
                    .Where(item => item.UserId == CurrentUserId && item.IsActive && item.IsSupportSpecialist)
                    .Select(item => item.ProductId)
                    .ToHashSet();
                var productTickets = (await _ticketService.GetAllAsync())
                    .Where(ticket => productIds.Contains(ticket.ProductId))
                    .OrderByDescending(ticket => ticket.CreatedDate)
                    .ThenByDescending(ticket => ticket.Id)
                    .ToList();
                ViewData["Title"] = "Ürün Talepleri";
                await PrepareTicketSlaListAsync(productTickets);
                return View(productTickets);
            }

            if (User.IsInRole(RoleNames.ProductManager))
            {
                ViewData["Title"] = "Talep Listesi";
                var responsibilities = (await _userProductService.GetAllAsync())
                    .Where(item => item.UserId == CurrentUserId && item.IsActive)
                    .ToList();
                var visibleProductIds = responsibilities
                    .Where(item => item.IsProductManager || item.IsSupportSpecialist)
                    .Select(item => item.ProductId)
                    .ToHashSet();
                var visibleTickets = (await _ticketService.GetAllAsync())
                    .Where(ticket => visibleProductIds.Contains(ticket.ProductId) && ticket.Status != TicketStatus.New)
                    .OrderByDescending(ticket => ticket.CreatedDate)
                    .ThenByDescending(ticket => ticket.Id)
                    .ToList();
                await PrepareTicketSlaListAsync(visibleTickets);
                return View(visibleTickets);
            }

            ViewData["Title"] = "Talep Listesi";
            var tickets = await _ticketService.GetAllAsync();
            await PrepareTicketSlaListAsync(tickets);
            return View(tickets);
        }

        [Authorize(Roles = $"{RoleNames.SupportSpecialist},{RoleNames.ProductManager}")]
        public async Task<IActionResult> MyTickets()
        {
            int userId = CurrentUserId;

            var tickets = await _ticketService.GetByAssignedUserAsync(userId);
            tickets = tickets
                .OrderByDescending(ticket => ticket.CreatedDate)
                .ThenByDescending(ticket => ticket.Id)
                .ToList();

            await PrepareTicketSlaListAsync(tickets);
            return View(tickets);
        }

        [Authorize(Roles = RoleNames.ProductManager)]
        public async Task<IActionResult> SupportPool()
        {
            var tickets = (await _ticketService.GetAllAsync(null, CurrentUserId, RoleNames.ProductManager))
                .Where(ticket => ticket.Status == TicketStatus.SupportQueue && !ticket.AssignedUserId.HasValue)
                .OrderBy(ticket => ticket.SlaLevel?.ResolutionTargetMinutes ?? int.MaxValue)
                .ThenBy(ticket => ticket.CreatedDate)
                .ToList();

            await PrepareTicketSlaListAsync(tickets);
            return View(tickets);
        }

        [Authorize(Roles = RoleNames.CompanyManager)]
        public async Task<IActionResult> CompanyTickets()
        {
            int tenantId = CurrentTenantId;

            var tickets = await _ticketService.GetByTenantAsync(tenantId);

            await PrepareTicketSlaListAsync(tickets);
            return View(tickets);
        }

        private async Task PrepareTicketSlaListAsync(List<Ticket> tickets)
        {
            var pauses = await _slaPauseService.GetByTicketIdsAsync(tickets.Select(t => t.Id));
            var pausesByTicket = pauses
                .GroupBy(p => p.TicketId)
                .ToDictionary(g => g.Key, g => g.Select(p => (p.StartDate, p.EndDate)).ToList());
            var items = new Dictionary<int, TicketSlaListItemViewModel>();

            foreach (var ticket in tickets)
            {
                if (ticket.SlaLevel == null || ticket.Tenant == null)
                    continue;

                pausesByTicket.TryGetValue(ticket.Id, out var ticketPauses);
                var tracking = SlaCalculator.CalculateTracking(
                    ticket,
                    ticket.SlaLevel,
                    ticket.Tenant.SLACalculationMethod,
                    ticketPauses);

                var terminal = ticket.Status == TicketStatus.Resolved ||
                    ticket.Status == TicketStatus.Closed ||
                    ticket.Status == TicketStatus.Cancelled;
                var breached = tracking.IsResponseBreached || tracking.IsResolutionBreached;
                var warning = tracking.IsStarted && !terminal && !breached &&
                    ((!tracking.IsResponseCompleted &&
                      tracking.ResponseRemaining.TotalMinutes <= ticket.SlaLevel.ResponseTargetMinutes * 0.25) ||
                     (!tracking.IsResolutionCompleted &&
                      tracking.ResolutionRemaining.TotalMinutes <= ticket.SlaLevel.ResolutionTargetMinutes * 0.25));

                var state = !tracking.IsStarted ? "not-started" : terminal ? "completed" : breached ? "breached" : warning ? "warning" : "active";
                var cssClass = !tracking.IsStarted ? "text-secondary" : terminal ? "text-secondary" : breached ? "text-danger" : warning ? "text-warning" : "text-success";

                items[ticket.Id] = new TicketSlaListItemViewModel
                {
                    LevelName = ticket.SlaLevel.Name,
                    State = state,
                    CssClass = cssClass,
                    ResponseText = !tracking.IsStarted ? "Desteğe yönlendirilmesi bekleniyor" : FormatSlaListText(
                        tracking.ResponseElapsed,
                        tracking.ResponseRemaining,
                        tracking.IsResponseCompleted,
                        tracking.IsResponseStopped,
                        tracking.IsResponseBreached),
                    ResolutionText = !tracking.IsStarted ? "Desteğe yönlendirilmesi bekleniyor" : FormatSlaListText(
                        tracking.ResolutionElapsed,
                        tracking.ResolutionRemaining,
                        tracking.IsResolutionCompleted,
                        tracking.IsResolutionStopped,
                        tracking.IsResolutionBreached)
                };

                items[ticket.Id].Summary = !tracking.IsStarted
                    ? "SLA başlamadı"
                    : breached
                    ? "SLA ihlali"
                    : warning
                        ? "Kritik süre"
                        : terminal
                            ? "Tamamlandı"
                            : "Süre devam ediyor";
            }

            ViewBag.TicketSlaItems = items;
        }

        private static string FormatSlaListText(
            TimeSpan elapsed,
            TimeSpan remaining,
            bool completed,
            bool stopped,
            bool breached)
        {
            if (stopped)
                return "Durduruldu";

            if (completed)
                return $"{SlaDurationFormatter.FormatDuration(elapsed)} içinde tamamlandı";

            return breached
                ? $"{SlaDurationFormatter.FormatDuration(remaining)} gecikti"
                : $"{SlaDurationFormatter.FormatDuration(remaining)} kaldı";
        }

        [HttpGet]
        [Authorize(Roles = $"{RoleNames.SystemAdmin},{RoleNames.CompanyManager},{RoleNames.CompanyUser}")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Products = await GetTenantProductsAsync(CurrentTenantId);
            ViewBag.Categories = await _categoryService.GetAllAsync();
            ViewBag.CurrentTenantName = (await _tenantService.GetByIdAsync(CurrentTenantId))?.CompanyName ?? "";

            var slaLevels = await _slaLevelService.GetActiveByTenantAsync(CurrentTenantId);
            ViewBag.SlaLevels = slaLevels;

            if (!string.IsNullOrEmpty(Request.Query["dosya"]))
            {
                ViewBag.FileError = "Yüklemeye çalıştığınız dosya çok büyük. Maksimum dosya boyutu 20 MB'dır. Lütfen daha küçük bir dosya seçerek tekrar deneyiniz.";
            }

            return View();
        }

        [HttpPost]
        [Authorize(Roles = $"{RoleNames.SystemAdmin},{RoleNames.CompanyManager},{RoleNames.CompanyUser}")]
        public async Task<IActionResult> Create(
            [Bind("Title", "Description", "Priority", "SlaLevelId", "ProductId", "CategoryId")] Ticket ticket, IFormFile? File)
        {
            ticket.Status = User.IsInRole(RoleNames.CompanyManager)
                ? TicketStatus.SupportQueue
                : TicketStatus.New;
            ticket.CreatedDate = DateTime.Now;
            ticket.SlaStartedDate = User.IsInRole(RoleNames.CompanyUser)
                ? null
                : ticket.CreatedDate;
            ticket.CreatedByUserId = CurrentUserId;
            ticket.TenantId = CurrentTenantId;

            if (ticket.TenantId == 0)
            {
                ModelState.AddModelError("", "Kullanıcı bir firmaya bağlı değil. Talep oluşturulamaz.");
                ViewBag.Products = new List<Product>();
                ViewBag.Categories = await _categoryService.GetAllAsync();
                ViewBag.CurrentTenantName = "";
                ViewBag.SlaLevels = new List<SlaLevel>();
                ViewBag.SelectedSlaLevelId = 0;
                return View(ticket);
            }

            ModelState.Remove(nameof(Ticket.TenantId));

            if (ticket.ProductId > 0)
            {
                var tenantProducts = await GetTenantProductsAsync(CurrentTenantId);
                if (!tenantProducts.Any(product => product.Id == ticket.ProductId))
                {
                    ModelState.AddModelError(
                        nameof(Ticket.ProductId),
                        "Seçilen ürün firmanızla ilişkilendirilmemiştir.");
                }
            }

            if (CurrentTenantId != 0 && ticket.SlaLevelId > 0)
            {
                var slaLevel = await _slaLevelService.GetByIdAsync(ticket.SlaLevelId);

                if (slaLevel == null || slaLevel.TenantId != CurrentTenantId)
                {
                    ModelState.AddModelError(nameof(Ticket.SlaLevelId), "Geçersiz SLA seviyesi.");
                }
            }

            if (File != null)
            {
                var fileValidation = ValidateFile(File);
                if (!fileValidation.Success)
                {
                    ModelState.AddModelError("", $"Dosya yüklenemedi: {fileValidation.Error}");
                }
            }

            if (ModelState.IsValid)
            {
                var recentDuplicate = (await _ticketService.GetAllAsync())
                    .FirstOrDefault(t =>
                        t.CreatedByUserId == CurrentUserId &&
                        t.Title == ticket.Title &&
                        t.ProductId == ticket.ProductId &&
                        t.CreatedDate >= DateTime.Now.AddSeconds(-10));

                if (recentDuplicate != null)
                {
                    TempData["Success"] = "Ticket başarıyla oluşturuldu.";
                    return RedirectToAction(nameof(Index));
                }

                await _ticketService.AddAsync(ticket);

                
                if (User.IsInRole(RoleNames.CompanyUser))
                {
                    var companyManagerIds = (await _userService.GetAllAsync())
                        .Where(user => user.IsActive &&
                            user.TenantId == ticket.TenantId &&
                            user.Role?.Name == RoleNames.CompanyManager)
                        .Select(user => user.Id)
                        .Distinct()
                        .ToList();

                    await _notificationService.SendToUsersAsync(
                        companyManagerIds,
                        $"Firmanız için yeni bir talep oluşturuldu: {ticket.Title} (#{ticket.Id})",
                        ticket.Id);
                }
                else if (ticket.Status == TicketStatus.SupportQueue)
                {
                    await NotifyProductManagersAsync(ticket, "Yeni destek talebi oluşturuldu");
                }

                if (File != null)
                {
                    await SaveAttachmentAsync(ticket.Id, File);
                }

                await _ticketHistoryService.AddAsync(new TicketHistory
                {
                    TicketId = ticket.Id,
                    UserId = ticket.CreatedByUserId,
                    Action = "Talep oluşturuldu",
                    NewValue = ticket.Status
                });

                TempData["Success"] = File != null ? "Ticket oluşturuldu ve dosya yüklendi." : "Ticket başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Products = await GetTenantProductsAsync(CurrentTenantId);
            ViewBag.Categories = await _categoryService.GetAllAsync();
            ViewBag.CurrentTenantName = "";

            var slaLevels = await _slaLevelService.GetActiveByTenantAsync(CurrentTenantId);
            ViewBag.SlaLevels = slaLevels;

            return View(ticket);
        }

        private async Task<List<Product>> GetTenantProductsAsync(int tenantId)
        {
            if (tenantId <= 0)
            {
                return new List<Product>();
            }

            return (await _tenantProductService.GetAllAsync())
                .Where(tenantProduct =>
                    tenantProduct.TenantId == tenantId &&
                    tenantProduct.Tenant?.IsActive == true &&
                    tenantProduct.IsActive &&
                    tenantProduct.Product?.IsActive == true)
                .Select(tenantProduct => tenantProduct.Product!)
                .GroupBy(product => product.Id)
                .Select(group => group.First())
                .OrderBy(product => product.Name)
                .ToList();
        }

        [HttpGet]
        [Authorize(Roles = $"{RoleNames.SystemAdmin},{RoleNames.CompanyManager}")]
        public async Task<IActionResult> Edit(int id, string? returnTo = null)
        {
            var ticket = await _ticketService.GetByIdAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            if (!await CanAccessTicketAsync(ticket))
            {
                return Forbid();
            }

            if (User.IsInRole(RoleNames.CompanyManager) && ticket.Status != TicketStatus.New)
            {
                TempData["Error"] = "Firma yöneticisi yalnızca Yeni durumundaki talepleri düzenleyebilir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.ReturnToCompanyTickets =
                User.IsInRole(RoleNames.CompanyManager) && returnTo == "company-tickets";

            ViewBag.Tenants = await _tenantService.GetAllAsync();
            ViewBag.Products = await _productService.GetAllAsync();
            ViewBag.Categories = await _categoryService.GetAllAsync();
            ViewBag.SlaLevels = await _slaLevelService.GetByTenantAsync(ticket.TenantId);
            ViewBag.SelectedSlaLevelId = ticket.SlaLevelId;

            return View(ticket);
        }

        [HttpPost]
        [Authorize(Roles = $"{RoleNames.SystemAdmin},{RoleNames.CompanyManager}")]
        public async Task<IActionResult> Edit(
            [Bind("Id", "Title", "Description", "Priority", "ProductId", "CategoryId", "SlaLevelId")] Ticket ticket,
            string? returnTo = null)
        {
            var existing = await _ticketService.GetByIdAsync(ticket.Id);

            if (existing == null)
            {
                return NotFound();
            }

            if (!await CanAccessTicketAsync(existing))
            {
                return Forbid();
            }

            if (User.IsInRole(RoleNames.CompanyManager) && existing.Status != TicketStatus.New)
            {
                TempData["Error"] = "Firma yöneticisi yalnızca Yeni durumundaki talepleri düzenleyebilir.";
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var returnToCompanyTickets =
                User.IsInRole(RoleNames.CompanyManager) && returnTo == "company-tickets";
            ViewBag.ReturnToCompanyTickets = returnToCompanyTickets;

            if (existing.Status == TicketStatus.Cancelled)
            {
                TempData["Error"] = "İptal edilen talepler güncellenemez.";
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            ModelState.Remove(nameof(Ticket.TenantId));

            if (ticket.SlaLevelId > 0)
            {
                var slaLevel = await _slaLevelService.GetByIdAsync(ticket.SlaLevelId);

                if (slaLevel == null || slaLevel.TenantId != existing.TenantId)
                {
                    ModelState.AddModelError(nameof(Ticket.SlaLevelId), "Geçersiz SLA seviyesi.");
                }
            }

            if (ModelState.IsValid)
            {
                var oldTitle = existing.Title;
                var oldDescription = existing.Description;
                var oldPriority = existing.Priority;
                var oldProductId = existing.ProductId;
                var oldProductName = existing.Product?.Name;
                var oldCategoryId = existing.CategoryId;
                var oldCategoryName = existing.Category?.Name;
                var oldSlaLevelId = existing.SlaLevelId;
                var oldSlaLevelName = existing.SlaLevel?.Name;

                existing.Title = ticket.Title;
                existing.Description = ticket.Description;
                existing.Priority = ticket.Priority;
                existing.ProductId = ticket.ProductId;
                existing.CategoryId = ticket.CategoryId;
                existing.SlaLevelId = ticket.SlaLevelId;

                await _ticketService.UpdateAsync(existing);

                if (!string.Equals((oldTitle ?? "").Trim(), (ticket.Title ?? "").Trim(), StringComparison.Ordinal))
                {
                    await AddEditHistoryAsync(existing.Id, "Başlık", oldTitle, ticket.Title);
                }

                if (!string.Equals((oldDescription ?? "").Trim(), (ticket.Description ?? "").Trim(), StringComparison.Ordinal))
                {
                    await AddEditHistoryAsync(existing.Id, "Açıklama", oldDescription, ticket.Description);
                }

                if (!string.Equals((oldPriority ?? "").Trim(), (ticket.Priority ?? "").Trim(), StringComparison.Ordinal))
                {
                    await AddEditHistoryAsync(existing.Id, "Öncelik", oldPriority, ticket.Priority);
                }

                if (oldProductId != ticket.ProductId)
                {
                    var newProduct = await _productService.GetByIdAsync(ticket.ProductId);
                    await AddEditHistoryAsync(
                        existing.Id,
                        "Ürün",
                        oldProductName ?? oldProductId.ToString(),
                        newProduct?.Name ?? ticket.ProductId.ToString());
                }

                if (oldCategoryId != ticket.CategoryId)
                {
                    var newCategory = await _categoryService.GetByIdAsync(ticket.CategoryId);
                    await AddEditHistoryAsync(
                        existing.Id,
                        "Kategori",
                        oldCategoryName ?? oldCategoryId.ToString(),
                        newCategory?.Name ?? ticket.CategoryId.ToString());
                }

                if (oldSlaLevelId != ticket.SlaLevelId)
                {
                    var newSlaLevel = await _slaLevelService.GetByIdAsync(ticket.SlaLevelId);
                    await AddEditHistoryAsync(
                        existing.Id,
                        "SLA Seviyesi",
                        oldSlaLevelName ?? oldSlaLevelId.ToString(),
                        newSlaLevel?.Name ?? ticket.SlaLevelId.ToString());
                }

                TempData["Success"] = "Ticket başarıyla güncellendi.";
                return returnToCompanyTickets
                    ? RedirectToAction(nameof(CompanyTickets))
                    : RedirectToAction(nameof(Index));
            }

            ViewBag.Tenants = await _tenantService.GetAllAsync();
            ViewBag.Products = await _productService.GetAllAsync();
            ViewBag.Categories = await _categoryService.GetAllAsync();
            ViewBag.SlaLevels = await _slaLevelService.GetByTenantAsync(existing.TenantId);
            ViewBag.SelectedSlaLevelId = ticket.SlaLevelId;

            return View(ticket);
        }

        private async Task AddEditHistoryAsync(int ticketId, string fieldName, string? oldValue, string? newValue)
        {
            await _ticketHistoryService.AddAsync(new TicketHistory
            {
                TicketId = ticketId,
                UserId = CurrentUserId,
                Action = $"Talep güncellendi: {fieldName}",
                OldValue = oldValue,
                NewValue = newValue
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, string? returnTo = null)
        {
            var ticket = await _ticketService.GetByIdAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            if (!await CanAccessTicketAsync(ticket))
            {
                return Forbid();
            }

            ViewBag.ReturnToCompanyTickets =
                User.IsInRole(RoleNames.CompanyManager) && returnTo == "company-tickets";
            ViewBag.ReturnToSupportPool =
                User.IsInRole(RoleNames.ProductManager) && returnTo == "support-pool";
            ViewBag.ReturnToMyTickets =
                (User.IsInRole(RoleNames.SupportSpecialist) || User.IsInRole(RoleNames.ProductManager)) &&
                returnTo == "assigned-tickets";

            ViewBag.Comments = await _ticketCommentService.GetByTicketIdAsync(id);
            ViewBag.Attachments = await _ticketAttachmentService.GetByTicketIdAsync(id);
            ViewBag.History = await _ticketHistoryService.GetByTicketIdAsync(id);
            ViewBag.Users = await GetAssignableUsersAsync(ticket);
            var productManagers = await GetProductManagersForProductAsync(ticket.ProductId);
            ViewBag.ProductManagers = productManagers;
            ViewBag.CurrentUserId = CurrentUserId;
            var productManagerCanManage = !User.IsInRole(RoleNames.ProductManager) ||
                await CanProductManagerManageAsync(ticket);
            ViewBag.CanModifyTicket = (!User.IsInRole(RoleNames.SupportSpecialist) ||
                ticket.AssignedUserId == CurrentUserId) && productManagerCanManage;

            if (ticket.SlaLevel != null && ticket.Tenant != null)
            {
                var pauses = (await _slaPauseService.GetByTicketIdAsync(id))
                    .Select(p => (p.StartDate, p.EndDate))
                    .ToList();

                ViewBag.SlaTracking = SlaCalculator.CalculateTracking(
                    ticket,
                    ticket.SlaLevel,
                    ticket.Tenant.SLACalculationMethod,
                    pauses);
            }

            if (TicketStatus.AllowedTransitions.TryGetValue(ticket.Status, out var allowed))
            {
                ViewBag.AllowedStatuses = allowed
                    .Where(status => status != TicketStatus.Assigned)
                    .Select(s => new KeyValuePair<string, string>(s, TicketStatus.DisplayNames[s]))
                    .ToList();
            }
            else
            {
                ViewBag.AllowedStatuses = new List<KeyValuePair<string, string>>();
            }

            var canActAsTicketOwner =
                User.IsInRole(RoleNames.SystemAdmin) ||
                User.IsInRole(RoleNames.CompanyManager) ||
                (User.IsInRole(RoleNames.CompanyUser) && ticket.CreatedByUserId == CurrentUserId);

            ViewBag.CanCancel = TicketStatus.CanCancel(ticket.Status) && canActAsTicketOwner;
            ViewBag.CanReopen = ticket.Status == TicketStatus.Closed && canActAsTicketOwner;

            ViewBag.CanCompanyRoute = User.IsInRole(RoleNames.CompanyManager) && ticket.Status == TicketStatus.New;
            ViewBag.CanCompanyClose = User.IsInRole(RoleNames.CompanyManager) &&
                (ticket.Status == TicketStatus.New || ticket.Status == TicketStatus.Resolved);
            ViewBag.CanAssign = (User.IsInRole(RoleNames.SystemAdmin) ||
                (User.IsInRole(RoleNames.ProductManager) && await IsManagerOfProductAsync(ticket.ProductId))) &&
                ticket.Status != TicketStatus.Resolved &&
                ticket.Status != TicketStatus.Closed &&
                ticket.Status != TicketStatus.Cancelled;
            ViewBag.CanTransferToProductManager = User.IsInRole(RoleNames.SupportSpecialist) &&
                ticket.AssignedUserId == CurrentUserId &&
                productManagers.Count > 0 &&
                ticket.Status != TicketStatus.Resolved &&
                ticket.Status != TicketStatus.Closed &&
                ticket.Status != TicketStatus.Cancelled;

            return View(ticket);
        }

        [HttpGet]
        public async Task<IActionResult> SlaTracking(int id)
        {
            var ticket = await _ticketService.GetByIdAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            if (!await CanAccessTicketAsync(ticket))
            {
                return Forbid();
            }

            if (ticket.SlaLevel == null || ticket.Tenant == null)
            {
                return NotFound();
            }

            var pauses = (await _slaPauseService.GetByTicketIdAsync(id))
                .Select(p => (p.StartDate, p.EndDate))
                .ToList();
            var tracking = SlaCalculator.CalculateTracking(
                ticket,
                ticket.SlaLevel,
                ticket.Tenant.SLACalculationMethod,
                pauses);
            var isPaused = ticket.Status == TicketStatus.WaitingCustomer;
            var isTerminal = ticket.Status == TicketStatus.Resolved ||
                ticket.Status == TicketStatus.Closed ||
                ticket.Status == TicketStatus.Cancelled;

            return Json(new
            {
                response = new
                {
                    isStarted = tracking.IsStarted,
                    remainingSeconds = tracking.ResponseRemaining.TotalSeconds,
                    elapsedSeconds = tracking.ResponseElapsed.TotalSeconds,
                    isCompleted = tracking.IsResponseCompleted,
                    isStopped = tracking.IsResponseStopped,
                    isBreached = tracking.IsResponseBreached,
                    shouldTick = tracking.IsStarted && !tracking.IsResponseCompleted && !tracking.IsResponseStopped && !isPaused && !isTerminal
                },
                resolution = new
                {
                    isStarted = tracking.IsStarted,
                    remainingSeconds = tracking.ResolutionRemaining.TotalSeconds,
                    elapsedSeconds = tracking.ResolutionElapsed.TotalSeconds,
                    isCompleted = tracking.IsResolutionCompleted,
                    isStopped = tracking.IsResolutionStopped,
                    isBreached = tracking.IsResolutionBreached,
                    shouldTick = tracking.IsStarted && !tracking.IsResolutionCompleted && !tracking.IsResolutionStopped && !isPaused && !isTerminal
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> UploadAttachment(UploadAttachmentViewModel model)
        {
            var ticket = await _ticketService.GetByIdAsync(model.TicketId);

            if (ticket == null)
            {
                return NotFound();
            }

            if (!await CanAccessTicketAsync(ticket))
            {
                return Forbid();
            }

            if (User.IsInRole(RoleNames.SupportSpecialist) && ticket.AssignedUserId != CurrentUserId)
            {
                return Forbid();
            }
            if (User.IsInRole(RoleNames.ProductManager) && !await CanProductManagerManageAsync(ticket)) return Forbid();

            if (ticket.Status == TicketStatus.Cancelled || ticket.Status == TicketStatus.Closed)
            {
                TempData["Error"] = "Kapatılmış veya iptal edilmiş taleplere dosya yüklenemez.";
                return RedirectToAction(nameof(Details), new { id = model.TicketId });
            }

            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError("File", "Lütfen bir dosya seçiniz.");
                TempData["UploadError"] = "Lütfen bir dosya seçiniz.";
                return RedirectToAction(nameof(Details), new { id = model.TicketId });
            }

            var result = await SaveAttachmentAsync(model.TicketId, model.File);

            if (!result.Success)
            {
                ModelState.AddModelError("File", result.Error ?? "Dosya yüklenemedi.");
                TempData["UploadError"] = result.Error;
                return RedirectToAction(nameof(Details), new { id = model.TicketId });
            }

            if (ticket != null)
            {
                var isCustomerResponse = ticket.Status == TicketStatus.WaitingCustomer &&
                    (User.IsInRole(RoleNames.CompanyUser) || User.IsInRole(RoleNames.CompanyManager));

                if (isCustomerResponse)
                {
                    await ResumeTicketAfterCustomerResponseAsync(ticket, "Müşteri dosya ekledi");

                    if (ticket.AssignedUserId.HasValue && ticket.AssignedUserId.Value != CurrentUserId)
                    {
                        await _notificationService.SendToUserAsync(
                            ticket.AssignedUserId.Value,
                            $"Müşteri yanıtladı ve dosya ekledi: {ticket.Title} (#{ticket.Id})",
                            ticket.Id);
                    }
                    else if (!ticket.AssignedUserId.HasValue)
                    {
                        await NotifyProductManagersAsync(ticket, "Müşteri yanıtladı ve dosya ekledi");
                    }
                }
                else if (ticket.CreatedByUserId.HasValue &&
                    ticket.CreatedByUserId.Value == CurrentUserId)
                {
                    if (ticket.AssignedUserId.HasValue &&
                        ticket.AssignedUserId.Value != CurrentUserId)
                    {
                        await _notificationService.SendToUserAsync(
                            ticket.AssignedUserId.Value,
                            $"Dosya yüklendi: {ticket.Title} (#{ticket.Id})",
                            ticket.Id);
                    }
                }
                else if (ticket.CreatedByUserId.HasValue)
                {
                    await _notificationService.SendToUserAsync(
                        ticket.CreatedByUserId.Value,
                        $"Dosya yüklendi: {ticket.Title} (#{ticket.Id})",
                        ticket.Id);
                }
            }

            TempData["UploadSuccess"] = "Dosya başarıyla yüklendi.";
            TempData["Success"] = "Dosya yüklendi.";

            return RedirectToAction(nameof(Details), new { id = model.TicketId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int ticketId, int attachmentId)
        {
            var ticket = await _ticketService.GetByIdAsync(ticketId);
            if (ticket == null)
            {
                return NotFound();
            }

            if (!await CanAccessTicketAsync(ticket))
            {
                return Forbid();
            }

            var attachment = (await _ticketAttachmentService.GetByTicketIdAsync(ticketId))
                .FirstOrDefault(item => item.Id == attachmentId);
            if (attachment == null)
            {
                return NotFound();
            }

            var storedFileName = Path.GetFileName(attachment.FilePath);
            var privatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "App_Data",
                "TicketAttachments",
                storedFileName);
            var legacyPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                storedFileName);
            var physicalPath = System.IO.File.Exists(privatePath) ? privatePath : legacyPath;

            if (!System.IO.File.Exists(physicalPath))
            {
                return NotFound();
            }

            return PhysicalFile(physicalPath, "application/octet-stream", attachment.FileName);
        }


        [HttpPost]
        public async Task<IActionResult> AddComment(AddCommentViewModel model)
        {
            var ticket = await _ticketService.GetByIdAsync(model.TicketId);

            if (ticket == null)
            {
                return NotFound();
            }

            if (!await CanAccessTicketAsync(ticket))
            {
                return Forbid();
            }

            if (User.IsInRole(RoleNames.SupportSpecialist) && ticket.AssignedUserId != CurrentUserId)
            {
                return Forbid();
            }
            if (User.IsInRole(RoleNames.ProductManager) && !await CanProductManagerManageAsync(ticket)) return Forbid();

            if (ticket.Status == TicketStatus.Cancelled || ticket.Status == TicketStatus.Closed)
            {
                TempData["Error"] = "Kapatılmış veya iptal edilmiş taleplere mesaj eklenemez.";
                return RedirectToAction(nameof(Details), new { id = model.TicketId });
            }

            if (string.IsNullOrWhiteSpace(model.Message) || model.Message.Trim().Length < 2)
            {
                TempData["Error"] = "Mesaj en az 2 karakter olmalıdır.";
                return RedirectToAction(nameof(Details), new { id = model.TicketId });
            }

            if (ModelState.IsValid)
            {
                var commentDate = DateTime.Now;
                var comment = new TicketComment
                {
                    TicketId = model.TicketId,
                    Message = model.Message.Trim(),
                    CreatedDate = commentDate,
                    UserId = CurrentUserId
                };

                await _ticketCommentService.AddAsync(comment);

                var isSupportResponse =
                    User.IsInRole(RoleNames.SupportSpecialist) ||
                    User.IsInRole(RoleNames.ProductManager);

                if (isSupportResponse && !ticket.FirstResponseDate.HasValue)
                {
                    ticket.FirstResponseDate = commentDate;
                    await _ticketService.UpdateAsync(ticket);

                    await _ticketHistoryService.AddAsync(new TicketHistory
                    {
                        TicketId = model.TicketId,
                        UserId = CurrentUserId,
                        Action = "İlk müdahale yapıldı",
                        NewValue = commentDate.ToString("dd.MM.yyyy HH:mm:ss")
                    });
                }

                await _ticketHistoryService.AddAsync(new TicketHistory
                {
                    TicketId = model.TicketId,
                    UserId = comment.UserId,
                    Action = "Mesaj eklendi",
                    NewValue = comment.Message
                });

                if (ticket != null)
                {
                    var isCustomerResponse = ticket.Status == TicketStatus.WaitingCustomer &&
                        (User.IsInRole(RoleNames.CompanyUser) || User.IsInRole(RoleNames.CompanyManager));

                    if (isCustomerResponse)
                    {
                        await ResumeTicketAfterCustomerResponseAsync(ticket, "Müşteri mesaj ekledi");

                        if (ticket.AssignedUserId.HasValue && ticket.AssignedUserId.Value != CurrentUserId)
                        {
                            await _notificationService.SendToUserAsync(
                                ticket.AssignedUserId.Value,
                                $"Müşteri yanıtladı: {ticket.Title} (#{ticket.Id})",
                                ticket.Id);
                        }
                        else if (!ticket.AssignedUserId.HasValue)
                        {
                            await NotifyProductManagersAsync(ticket, "Müşteri yanıtladı");
                        }
                    }
                    else if (ticket.CreatedByUserId.HasValue &&
                        ticket.CreatedByUserId.Value == CurrentUserId)
                    {
                        if (ticket.AssignedUserId.HasValue &&
                            ticket.AssignedUserId.Value != CurrentUserId)
                        {
                            await _notificationService.SendToUserAsync(
                                ticket.AssignedUserId.Value,
                                $"Mesaj eklendi: {ticket.Title} (#{ticket.Id})",
                                ticket.Id);
                        }
                    }
                    else if (ticket.CreatedByUserId.HasValue)
                    {
                        await _notificationService.SendToUserAsync(
                            ticket.CreatedByUserId.Value,
                            $"Mesaj eklendi: {ticket.Title} (#{ticket.Id})",
                            ticket.Id);
                    }
                }

                TempData["Success"] = "Mesaj gönderildi.";
            }

            return RedirectToAction(nameof(Details), new { id = model.TicketId });
        }

        [HttpPost]
        [Authorize(Roles = $"{RoleNames.SystemAdmin},{RoleNames.SupportSpecialist},{RoleNames.ProductManager},{RoleNames.CompanyManager}")]
        public async Task<IActionResult> ChangeStatus(int ticketId, string status)
        {
            var accessibleTicket = await _ticketService.GetByIdAsync(ticketId);
            if (accessibleTicket == null)
            {
                return NotFound();
            }

            if (!await CanAccessTicketAsync(accessibleTicket))
            {
                return Forbid();
            }

            if (User.IsInRole(RoleNames.SupportSpecialist) && accessibleTicket.AssignedUserId != CurrentUserId)
            {
                return Forbid();
            }
            if (User.IsInRole(RoleNames.ProductManager) && !await CanProductManagerManageAsync(accessibleTicket)) return Forbid();

            if (User.IsInRole(RoleNames.CompanyManager))
            {
                var allowedForCompanyManager =
                    (accessibleTicket.Status == TicketStatus.New &&
                        (status == TicketStatus.SupportQueue || status == TicketStatus.Closed)) ||
                    (accessibleTicket.Status == TicketStatus.Resolved && status == TicketStatus.Closed);

                if (!allowedForCompanyManager)
                {
                    TempData["Error"] = "Firma yöneticisi bu durum geçişini yapamaz.";
                    return RedirectToAction(nameof(Details), new { id = ticketId });
                }
            }

            if (User.IsInRole(RoleNames.ProductManager) &&
                accessibleTicket.Status == TicketStatus.New)
            {
                return Forbid();
            }

            if (status == TicketStatus.Assigned && !accessibleTicket.AssignedUserId.HasValue)
            {
                TempData["Error"] = "Atandı durumuna geçmek için önce bir destek uzmanı seçilmelidir.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            try
            {
                await _ticketService.ChangeStatusAsync(ticketId, status, CurrentUserId);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var ticket = await _ticketService.GetByIdAsync(ticketId);

            if (ticket != null)
            {
                if (status == TicketStatus.SupportQueue)
                {
                    await NotifyProductManagersAsync(ticket, "Talep destek havuzuna yönlendirildi");
                }
                var statusDisplay = TicketStatus.DisplayNames.TryGetValue(status, out var display)
                    ? display
                    : status;

                if (ticket.CreatedByUserId.HasValue &&
                    ticket.CreatedByUserId.Value != CurrentUserId)
                {
                    var ownerMessage = status switch
                    {
                        TicketStatus.Resolved => $"Talebiniz çözüldü. Kontrol edebilirsiniz: {ticket.Title} (#{ticket.Id})",
                        TicketStatus.Closed => $"Talebiniz kapatıldı: {ticket.Title} (#{ticket.Id})",
                        _ => $"Talep durumu güncellendi: {statusDisplay} — {ticket.Title} (#{ticket.Id})"
                    };

                    await _notificationService.SendToUserAsync(
                        ticket.CreatedByUserId.Value,
                        ownerMessage,
                        ticket.Id);
                }

                if (ticket.AssignedUserId.HasValue &&
                    ticket.AssignedUserId.Value != CurrentUserId &&
                    ticket.AssignedUserId.Value != ticket.CreatedByUserId)
                {
                    await _notificationService.SendToUserAsync(
                        ticket.AssignedUserId.Value,
                        $"Talep durumu güncellendi: {statusDisplay} — {ticket.Title} (#{ticket.Id})",
                        ticket.Id);
                }

            }

            TempData["Success"] = "Durum güncellendi.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        [HttpPost]
        [Authorize(Roles = $"{RoleNames.SystemAdmin},{RoleNames.CompanyManager},{RoleNames.CompanyUser}")]
        public async Task<IActionResult> Cancel(int ticketId)
        {
            var ticket = await _ticketService.GetByIdAsync(ticketId);

            if (ticket == null)
            {
                return NotFound();
            }

            if (!await CanAccessTicketAsync(ticket))
            {
                return Forbid();
            }

            if (User.IsInRole(RoleNames.CompanyUser) && ticket.CreatedByUserId != CurrentUserId)
            {
                TempData["Error"] = "Yalnızca kendi taleplerinizi iptal edebilirsiniz.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            try
            {
                await _ticketService.CancelAsync(ticketId, CurrentUserId);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var cancelMessage = $"Talep iptal edildi: {ticket.Title} (#{ticket.Id})";

            if (User.IsInRole(RoleNames.CompanyUser))
            {
                var companyManagerIds = (await _userService.GetAllAsync())
                    .Where(user => user.IsActive &&
                        user.TenantId == ticket.TenantId &&
                        user.Role?.Name == RoleNames.CompanyManager &&
                        user.Id != CurrentUserId)
                    .Select(user => user.Id)
                    .Distinct()
                    .ToList();

                await _notificationService.SendToUsersAsync(
                    companyManagerIds,
                    $"Firma kullanıcısı talebi iptal etti: {ticket.Title} (#{ticket.Id})",
                    ticket.Id);
            }

            if (ticket.CreatedByUserId.HasValue &&
                ticket.CreatedByUserId.Value != CurrentUserId)
            {
                await _notificationService.SendToUserAsync(
                    ticket.CreatedByUserId.Value,
                    cancelMessage,
                    ticket.Id);
            }

            if (ticket.AssignedUserId.HasValue &&
                ticket.AssignedUserId.Value != CurrentUserId &&
                ticket.AssignedUserId.Value != ticket.CreatedByUserId)
            {
                await _notificationService.SendToUserAsync(
                    ticket.AssignedUserId.Value,
                    cancelMessage,
                    ticket.Id);
            }

            TempData["Success"] = "Talep iptal edildi.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        [HttpPost]
        [Authorize(Roles = $"{RoleNames.SystemAdmin},{RoleNames.CompanyManager},{RoleNames.CompanyUser}")]
        public async Task<IActionResult> Reopen(int ticketId)
        {
            var ticket = await _ticketService.GetByIdAsync(ticketId);

            if (ticket == null)
            {
                return NotFound();
            }

            if (!await CanAccessTicketAsync(ticket))
            {
                return Forbid();
            }

            if (User.IsInRole(RoleNames.CompanyUser) && ticket.CreatedByUserId != CurrentUserId)
            {
                TempData["Error"] = "Yalnızca kendi taleplerinizi yeniden açabilirsiniz.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            try
            {
                await _ticketService.ReopenAsync(ticketId, CurrentUserId);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            ticket = await _ticketService.GetByIdAsync(ticketId);
            if (ticket != null)
            {
                var statusDisplay = TicketStatus.DisplayNames.TryGetValue(ticket.Status, out var display)
                    ? display
                    : ticket.Status;
                var reopenMessage = $"Talep yeniden açıldı ({statusDisplay}): {ticket.Title} (#{ticket.Id})";

                if (ticket.Status == TicketStatus.SupportQueue)
                {
                    await NotifyProductManagersAsync(ticket, "Kapatılan talep yeniden açıldı ve destek havuzuna düştü");
                }

                if (ticket.CreatedByUserId.HasValue &&
                    ticket.CreatedByUserId.Value != CurrentUserId)
                {
                    await _notificationService.SendToUserAsync(
                        ticket.CreatedByUserId.Value,
                        reopenMessage,
                        ticket.Id);
                }

                if (ticket.AssignedUserId.HasValue &&
                    ticket.AssignedUserId.Value != CurrentUserId &&
                    ticket.AssignedUserId.Value != ticket.CreatedByUserId)
                {
                    await _notificationService.SendToUserAsync(
                        ticket.AssignedUserId.Value,
                        reopenMessage,
                        ticket.Id);
                }
            }

            TempData["Success"] = "Talep yeniden açıldı.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        [HttpPost]
        [Authorize(Roles = $"{RoleNames.SystemAdmin},{RoleNames.ProductManager}")]
        public async Task<IActionResult> AssignUser(int ticketId, int userId)
        {
            var accessibleTicket = await _ticketService.GetByIdAsync(ticketId);
            if (accessibleTicket == null)
            {
                return NotFound();
            }

            if (!await CanAccessTicketAsync(accessibleTicket))
            {
                return Forbid();
            }

            if (User.IsInRole(RoleNames.ProductManager) &&
                !await IsManagerOfProductAsync(accessibleTicket.ProductId))
            {
                return Forbid();
            }

            if (accessibleTicket.Status == TicketStatus.Resolved ||
                accessibleTicket.Status == TicketStatus.Closed ||
                accessibleTicket.Status == TicketStatus.Cancelled)
            {
                TempData["Error"] = "Tamamlanmış veya iptal edilmiş taleplerde atama yapılamaz.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var assignableUsers = await GetAssignableUsersAsync(accessibleTicket);
            var assignedUser = assignableUsers.FirstOrDefault(user => user.Id == userId);
            if (assignedUser == null)
            {
                TempData["Error"] = "Bu kullanıcıya talep atama yetkiniz bulunmuyor.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            await _ticketService.AssignUserAsync(ticketId, userId, CurrentUserId);

            if (accessibleTicket.Status == TicketStatus.New ||
                accessibleTicket.Status == TicketStatus.SupportQueue)
            {
                await _ticketService.ChangeStatusAsync(ticketId, TicketStatus.Assigned, CurrentUserId);
            }

            var ticket = await _ticketService.GetByIdAsync(ticketId);

            if (ticket != null &&
                ticket.AssignedUserId.HasValue &&
                ticket.AssignedUserId.Value != CurrentUserId)
            {
                await _notificationService.SendToUserAsync(
                    ticket.AssignedUserId.Value,
                    $"Size yeni bir talep atandı: {ticket.Title} (#{ticket.Id})",
                    ticket.Id);
            }

            TempData["Success"] = userId == CurrentUserId
                ? "Talep kendi üzerinize alındı."
                : "Destek uzmanı atandı.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.SupportSpecialist)]
        public async Task<IActionResult> TransferToProductManager(int ticketId, int productManagerId)
        {
            var ticket = await _ticketService.GetByIdAsync(ticketId);
            if (ticket == null)
            {
                return NotFound();
            }

            if (ticket.AssignedUserId != CurrentUserId || !await CanAccessTicketAsync(ticket))
            {
                return Forbid();
            }

            if (ticket.Status == TicketStatus.Resolved ||
                ticket.Status == TicketStatus.Closed ||
                ticket.Status == TicketStatus.Cancelled)
            {
                TempData["Error"] = "Tamamlanmış veya iptal edilmiş talepler devredilemez.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var productManager = (await GetProductManagersForProductAsync(ticket.ProductId))
                .FirstOrDefault(user => user.Id == productManagerId);
            if (productManager == null)
            {
                TempData["Error"] = "Bu ürün için geçerli bir ürün yöneticisi seçilmelidir.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            await _ticketService.AssignUserAsync(
                ticketId,
                productManager.Id,
                CurrentUserId,
                "Talep ürün yöneticisine devredildi");

            await _notificationService.SendToUserAsync(
                productManager.Id,
                $"Bir talep size devredildi: {ticket.Title} (#{ticket.Id})",
                ticket.Id);

            TempData["Success"] = "Talep ürün yöneticisine devredildi.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        private async Task<List<User>> GetProductManagersForProductAsync(int productId)
        {
            var managerIds = (await _userProductService.GetAllAsync())
                .Where(item => item.ProductId == productId && item.IsActive && item.IsProductManager)
                .Select(item => item.UserId)
                .ToHashSet();

            return (await _userService.GetAllAsync())
                .Where(user => user.IsActive &&
                    user.Role?.Name == RoleNames.ProductManager &&
                    managerIds.Contains(user.Id))
                .OrderBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .ToList();
        }

        private async Task<List<User>> GetAssignableUsersAsync(Ticket ticket)
        {
            var users = await _userService.GetAllAsync();
            var userProducts = await _userProductService.GetAllAsync();
            var specialistIds = userProducts
                .Where(item => item.ProductId == ticket.ProductId && item.IsActive && item.IsSupportSpecialist)
                .Select(item => item.UserId)
                .ToHashSet();
            var specialists = users
                .Where(user => user.IsActive &&
                    user.Role?.Name == RoleNames.SupportSpecialist &&
                    specialistIds.Contains(user.Id))
                .ToList();
            var productManagerIds = userProducts
                .Where(item => item.ProductId == ticket.ProductId && item.IsActive && item.IsProductManager)
                .Select(item => item.UserId)
                .ToHashSet();

            if (User.IsInRole(RoleNames.ProductManager))
            {
                specialists.AddRange(users.Where(user =>
                    user.IsActive && user.Role?.Name == RoleNames.ProductManager && specialistIds.Contains(user.Id)));
                var currentManager = users.FirstOrDefault(user =>
                    user.Id == CurrentUserId &&
                    user.IsActive &&
                    user.Role?.Name == RoleNames.ProductManager &&
                    productManagerIds.Contains(user.Id));
                if (currentManager != null) specialists.Add(currentManager);
            }
            else if (User.IsInRole(RoleNames.SystemAdmin))
            {
                specialists.AddRange(users.Where(user =>
                    user.IsActive &&
                    user.Role?.Name == RoleNames.ProductManager &&
                    (productManagerIds.Contains(user.Id) || specialistIds.Contains(user.Id))));
            }

            return specialists
                .GroupBy(user => user.Id)
                .Select(group => group.First())
                .OrderBy(user => user.Role?.Name == RoleNames.ProductManager ? 0 : 1)
                .ThenBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .ToList();
        }

        private async Task NotifyProductManagersAsync(Ticket ticket, string action)
        {
            var userProducts = await _userProductService.GetAllAsync();
            var productManagerIds = (await _userService.GetAllAsync())
                .Where(user => user.IsActive && user.Role?.Name == RoleNames.ProductManager)
                .Select(user => user.Id)
                .ToHashSet();
            var managerIds = userProducts
                .Where(item => item.ProductId == ticket.ProductId &&
                               item.IsActive && item.IsProductManager &&
                               productManagerIds.Contains(item.UserId))
                .Select(item => item.UserId)
                .Distinct()
                .ToList();

            await _notificationService.SendToUsersAsync(
                managerIds,
                $"{action}: {ticket.Title} (#{ticket.Id})",
                ticket.Id);
        }

        private async Task ResumeTicketAfterCustomerResponseAsync(Ticket ticket, string action)
        {
            if (ticket.Status != TicketStatus.WaitingCustomer)
            {
                return;
            }

            await _slaPauseService.StopPauseAsync(ticket.Id);

            ticket.Status = TicketStatus.InReview;
            await _ticketService.UpdateAsync(ticket);

            await _ticketHistoryService.AddAsync(new TicketHistory
            {
                TicketId = ticket.Id,
                UserId = CurrentUserId,
                Action = action + "; talep otomatik olarak incelemeye alındı",
                OldValue = TicketStatus.WaitingCustomer,
                NewValue = TicketStatus.InReview
            });
        }

        private async Task<bool> CanAccessTicketAsync(Ticket ticket)
        {
            if (User.IsInRole(RoleNames.SystemAdmin))
            {
                return true;
            }

            if (User.IsInRole(RoleNames.CompanyManager))
            {
                return ticket.TenantId == CurrentTenantId;
            }

            if (User.IsInRole(RoleNames.CompanyUser))
            {
                return ticket.TenantId == CurrentTenantId &&
                    ticket.CreatedByUserId == CurrentUserId;
            }

            if (User.IsInRole(RoleNames.SupportSpecialist))
            {
                if (ticket.AssignedUserId == CurrentUserId)
                {
                    return true;
                }

                var userProducts = await _userProductService.GetAllAsync();
                return userProducts.Any(up =>
                    up.UserId == CurrentUserId &&
                    up.ProductId == ticket.ProductId &&
                    up.IsActive && up.IsSupportSpecialist);
            }

            if (User.IsInRole(RoleNames.ProductManager))
            {
                if (ticket.AssignedUserId == CurrentUserId)
                {
                    return true;
                }

                var userProducts = await _userProductService.GetAllAsync();
                return userProducts.Any(up =>
                    up.UserId == CurrentUserId &&
                    up.ProductId == ticket.ProductId &&
                    up.IsActive && (up.IsProductManager || up.IsSupportSpecialist));
            }

            return false;
        }

        private const long MaxFileSize = 20 * 1024 * 1024;

        private async Task<bool> IsManagerOfProductAsync(int productId)
        {
            return (await _userProductService.GetAllAsync()).Any(item =>
                item.UserId == CurrentUserId && item.ProductId == productId &&
                item.IsActive && item.IsProductManager);
        }

        private async Task<bool> CanProductManagerManageAsync(Ticket ticket)
        {
            return ticket.AssignedUserId == CurrentUserId || await IsManagerOfProductAsync(ticket.ProductId);
        }

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".zip", ".log" };

        private (bool Success, string? Error) ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return (false, "Lütfen bir dosya seçiniz.");

            if (file.Length > MaxFileSize)
                return (false, "Dosya boyutu en fazla 20 MB olabilir.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return (false, "Bu dosya türü yüklenemez. İzin verilen türler: jpg, jpeg, png, gif, webp, pdf, doc, docx, xls, xlsx, txt, zip, log");

            return (true, null);
        }

        private async Task<(bool Success, string? Error)> SaveAttachmentAsync(int ticketId, IFormFile file)
        {
            var validation = ValidateFile(file);
            if (!validation.Success)
                return (false, validation.Error);

            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "App_Data",
                "TicketAttachments");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();

            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new TicketAttachment
            {
                TicketId = ticketId,
                FileName = file.FileName,
                FilePath = fileName,
                UploadedDate = DateTime.Now,
                UploadedByUserId = CurrentUserId
            };

            await _ticketAttachmentService.AddAsync(attachment);

            await _ticketHistoryService.AddAsync(new TicketHistory
            {
                TicketId = ticketId,
                UserId = CurrentUserId,
                Action = "Dosya yüklendi",
                NewValue = attachment.FileName
            });

            return (true, null);
        }
    }
}
