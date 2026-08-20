using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPanel.Constants;
using SupportPanel.Interfaces;
using SupportPanel.Models;
using SupportPanel.Services;
using SupportPanel.ViewModels;

namespace SupportPanel.Controllers
{
    [Authorize(Roles = $"{RoleNames.SystemAdmin},{RoleNames.ProductManager},{RoleNames.CompanyManager},{RoleNames.CompanyUser}")]
    public class ReportsController : Controller
    {
        private readonly ITicketService _ticketService;
        private readonly ISlaPauseService _slaPauseService;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly ITenantService _tenantService;
        private readonly ISlaLevelService _slaLevelService;
        private readonly IUserProductService _userProductService;

        private int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        private int CurrentTenantId => int.TryParse(User.FindFirstValue("TenantId"), out var id) ? id : 0;

        public ReportsController(
            ITicketService ticketService,
            ISlaPauseService slaPauseService,
            IProductService productService,
            ICategoryService categoryService,
            ITenantService tenantService,
            ISlaLevelService slaLevelService,
            IUserProductService userProductService)
        {
            _ticketService = ticketService;
            _slaPauseService = slaPauseService;
            _productService = productService;
            _categoryService = categoryService;
            _tenantService = tenantService;
            _slaLevelService = slaLevelService;
            _userProductService = userProductService;
        }

        public async Task<IActionResult> Index(
            DateTime? startDate,
            DateTime? endDate,
            string? product,
            string? category,
            string? slaLevel,
            string? status,
            string? slaState,
            string? tenant,
            string? search,
            List<string>? violationTypes,
            List<int>? assignedUserIds,
            List<string>? priorities)
        {
         
            var tickets = await GetScopedTicketsAsync();
            var allScopedTickets = tickets.ToList();

           
            var cancelledCount = allScopedTickets.Count(t => t.Status == TicketStatus.Cancelled);

            if (string.IsNullOrWhiteSpace(status))
            {
                tickets = tickets.Where(t => t.Status != TicketStatus.Cancelled).ToList();
            }

            var hasInvalidDateRange = startDate.HasValue && endDate.HasValue && endDate.Value.Date < startDate.Value.Date;
            if (hasInvalidDateRange)
                ViewData["DateRangeError"] = "Bitiş tarihi başlangıç tarihinden önce olamaz.";

            if (!hasInvalidDateRange)
            {
                if (startDate.HasValue)
                    tickets = tickets.Where(t => t.CreatedDate.Date >= startDate.Value.Date).ToList();
                if (endDate.HasValue)
                    tickets = tickets.Where(t => t.CreatedDate.Date <= endDate.Value.Date).ToList();
            }
            if (!string.IsNullOrWhiteSpace(search))
                tickets = tickets.Where(t => t.Title.Contains(search.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToList();
            if (!string.IsNullOrWhiteSpace(product))
                tickets = tickets.Where(t => t.Product?.Name == product).ToList();
            if (!string.IsNullOrWhiteSpace(category))
                tickets = tickets.Where(t => t.Category?.Name == category).ToList();
            if (priorities is { Count: > 0 })
            {
                var selectedPriorities = priorities
                    .Where(value => value is "Low" or "Medium" or "High" or "Critical")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                tickets = tickets.Where(t => selectedPriorities.Contains(t.Priority)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(slaLevel))
                tickets = tickets.Where(t => t.SlaLevel?.Name == slaLevel).ToList();
            if (!string.IsNullOrWhiteSpace(status))
                tickets = tickets.Where(t => t.Status == status).ToList();
            if (!string.IsNullOrWhiteSpace(tenant) &&
                (User.IsInRole(RoleNames.SystemAdmin) || User.IsInRole(RoleNames.ProductManager)))
                tickets = tickets.Where(t => t.Tenant?.CompanyName == tenant).ToList();
            if (assignedUserIds is { Count: > 0 })
            {
                var selectedAssigneeIds = assignedUserIds.ToHashSet();
                tickets = tickets.Where(t => t.AssignedUserId.HasValue
                    ? selectedAssigneeIds.Contains(t.AssignedUserId.Value)
                    : selectedAssigneeIds.Contains(0)).ToList();
            }

            var rows = await BuildReportRowsAsync(tickets);
            if (violationTypes is { Count: > 0 })
            {
                var selectedViolationTypes = violationTypes
                    .Where(value => value is "İlk müdahale" or "Çözüm")
                    .ToHashSet(StringComparer.CurrentCultureIgnoreCase);
                if (selectedViolationTypes.Contains("İlk müdahale") &&
                    selectedViolationTypes.Contains("Çözüm"))
                {
                    selectedViolationTypes.Add("İkisi birden");
                }
                var ids = rows.Where(r => selectedViolationTypes.Contains(r.ViolationType))
                    .Select(r => r.TicketId)
                    .ToHashSet();
                tickets = tickets.Where(t => ids.Contains(t.Id)).ToList();
                rows = rows.Where(r => ids.Contains(r.TicketId)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(slaState))
            {
                var ids = rows.Where(r => r.SlaState == slaState).Select(r => r.TicketId).ToHashSet();
                tickets = tickets.Where(t => ids.Contains(t.Id)).ToList();
                rows = rows.Where(r => ids.Contains(r.TicketId)).ToList();
            }

            var completedResponses = rows.Where(r => r.IsResponseCompleted).ToList();
            var completedResolutions = rows.Where(r => r.IsResolutionCompleted).ToList();
            var responseBreaches = rows.Count(r => r.ViolationType is "İlk müdahale" or "İkisi birden");
            var resolutionBreaches = rows.Count(r => r.ViolationType is "Çözüm" or "İkisi birden");

            var model = new ReportsDashboardViewModel
            {
                TotalTickets = allScopedTickets.Count,
                OpenTickets = tickets.Count(t => IsOpenStatus(t.Status)),
                ResolvedTickets = tickets.Count(t => t.Status == TicketStatus.Resolved),
                ClosedTickets = tickets.Count(t => t.Status == TicketStatus.Closed),
                CancelledTickets = allScopedTickets.Count(t => t.Status == TicketStatus.Cancelled),
                SlaBreachedTickets = rows.Count(r => r.SlaState == "breached"),
                ActiveSlaTickets = rows.Count(r => r.SlaState == "active"),
                WarningSlaTickets = rows.Count(r => r.SlaState == "warning"),
                ResponseBreachedTickets = responseBreaches,
                ResolutionBreachedTickets = resolutionBreaches,
                AverageResponseMinutes = completedResponses.Count == 0 ? 0 : completedResponses.Average(r => r.ResponseElapsedMinutes),
                AverageResolutionMinutes = completedResolutions.Count == 0 ? 0 : completedResolutions.Average(r => r.ResolutionElapsedMinutes),
                ResponseComplianceRate = completedResponses.Count == 0 ? 0 : 100.0 * completedResponses.Count(r => !r.IsResponseBreached) / completedResponses.Count,
                ResolutionComplianceRate = completedResolutions.Count == 0 ? 0 : 100.0 * completedResolutions.Count(r => !r.IsResolutionBreached) / completedResolutions.Count,
                TicketRows = rows,
                Products = allScopedTickets.Select(t => t.Product?.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).Cast<string>().ToList(),
                Categories = allScopedTickets.Select(t => t.Category?.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).Cast<string>().ToList(),
                SlaLevels = allScopedTickets.Select(t => t.SlaLevel?.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).Cast<string>().ToList(),
                Tenants = allScopedTickets.Select(t => t.Tenant?.CompanyName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).Cast<string>().ToList(),
                Assignees = allScopedTickets
                    .Where(t => t.AssignedUser != null)
                    .GroupBy(t => t.AssignedUserId!.Value)
                    .Select(group => new ReportAssigneeOption
                    {
                        Id = group.Key,
                        Name = $"{group.First().AssignedUser!.FirstName} {group.First().AssignedUser!.LastName}"
                    })
                    .OrderBy(option => option.Name)
                    .ToList(),
                RecentTickets = tickets
                    .OrderByDescending(t => t.CreatedDate)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }
        // 1. Ürün Talep Özeti (Kategori + Firma + SLA Seviyesi Filtreleri)
        [Authorize(Roles = $"{RoleNames.SystemAdmin},{RoleNames.ProductManager},{RoleNames.CompanyManager}")]
        public async Task<IActionResult> ProductCounts(string? category, string? tenant, string? slaLevel)
        {
            var tickets = await GetScopedTicketsAsync();

            // İptal edilen kayıtlar ürün özetine dahil edilmez.
            tickets = tickets.Where(ticket => ticket.Status != TicketStatus.Cancelled).ToList();

            var categories = (await _categoryService.GetAllAsync()).OrderBy(c => c.Name).Select(c => c.Name).ToList();
            var tenants = (await _tenantService.GetAllAsync()).OrderBy(t => t.CompanyName).Select(t => t.CompanyName).ToList();
            var slaLevels = (await _slaLevelService.GetAllAsync()).OrderBy(s => s.Name).Select(s => s.Name).ToList();

            var selectedCategory = category?.Trim();
            var selectedTenant = tenant?.Trim();
            var selectedSlaLevel = slaLevel?.Trim();

            if (!string.IsNullOrWhiteSpace(selectedCategory))
                tickets = tickets.Where(ticket => ticket.Category?.Name == selectedCategory).ToList();

            if (!string.IsNullOrWhiteSpace(selectedTenant))
                tickets = tickets.Where(ticket => ticket.Tenant?.CompanyName == selectedTenant).ToList();

            if (!string.IsNullOrWhiteSpace(selectedSlaLevel))
                tickets = tickets.Where(ticket => ticket.SlaLevel?.Name == selectedSlaLevel).ToList();

            ViewBag.Categories = categories;
            ViewBag.Tenants = tenants;
            ViewBag.SlaLevels = slaLevels;
            ViewBag.SelectedCategory = selectedCategory;
            ViewBag.SelectedTenant = selectedTenant;
            ViewBag.SelectedSlaLevel = selectedSlaLevel;

            var breachedIds = (await BuildReportRowsAsync(tickets))
                .Where(row => row.SlaState == "breached")
                .Select(row => row.TicketId)
                .ToHashSet();

            var managedProductIds = await GetManagedProductIdsAsync();
            var products = await _productService.GetAllAsync();

            if (User.IsInRole(RoleNames.ProductManager))
            {
                products = products.Where(product => managedProductIds.Contains(product.Id)).ToList();
            }

            var model = products
                .Select(product => new ProductCountReportViewModel
                {
                    ProductName = product.Name,
                    TotalCount = tickets.Count(ticket => ticket.ProductId == product.Id),
                    OpenCount = tickets.Count(ticket => ticket.ProductId == product.Id && IsOpenStatus(ticket.Status)),
                    CompletedCount = tickets.Count(ticket => ticket.ProductId == product.Id && (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed)),
                    AssignedCount = tickets.Count(ticket => ticket.ProductId == product.Id && IsOpenStatus(ticket.Status) && ticket.AssignedUserId.HasValue),
                    UnassignedCount = tickets.Count(ticket => ticket.ProductId == product.Id && IsOpenStatus(ticket.Status) && !ticket.AssignedUserId.HasValue),
                    SlaBreachedCount = tickets.Count(ticket => ticket.ProductId == product.Id && breachedIds.Contains(ticket.Id))
                })
                .OrderBy(row => row.ProductName)
                .ToList();

            return View(model);
        }

        [Authorize(Roles = $"{RoleNames.SystemAdmin},{RoleNames.ProductManager}")]
        public async Task<IActionResult> FirmReports()
        {
            var tickets = await GetScopedTicketsAsync();
            tickets = tickets.Where(t => t.Status != TicketStatus.Cancelled).ToList();
            var tenants = await _tenantService.GetAllAsync();

            var model = tenants
                .Select(t => new FirmReportViewModel
                {
                    CompanyName = t.CompanyName,
                    TotalCount = tickets.Count(x => x.TenantId == t.Id),
                    OpenCount = tickets.Count(x => x.TenantId == t.Id && IsOpenStatus(x.Status)),
                    ResolvedCount = tickets.Count(x => x.TenantId == t.Id && x.Status == TicketStatus.Resolved)
                })
                .OrderBy(r => r.CompanyName)
                .ToList();

            return View(model);
        }

        [Authorize(Roles = $"{RoleNames.SystemAdmin},{RoleNames.ProductManager}")]
        public async Task<IActionResult> ProductCompanyCounts()
        {
            var tickets = await GetScopedTicketsAsync();

            var model = tickets
                .GroupBy(ticket => new
                {
                    ProductName = ticket.Product?.Name ?? "-",
                    CompanyName = ticket.Tenant?.CompanyName ?? "-"
                })
                .Select(group => new ProductCompanyCountReportViewModel
                {
                    ProductName = group.Key.ProductName,
                    CompanyName = group.Key.CompanyName,
                    TotalCount = group.Count(),
                    OpenCount = group.Count(ticket => IsOpenStatus(ticket.Status)),
                    CompletedCount = group.Count(ticket =>
                        ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed),
                    CancelledCount = group.Count(t => t.Status == TicketStatus.Cancelled),
                    AssignedCount = group.Count(ticket =>
                        IsOpenStatus(ticket.Status) && ticket.AssignedUserId.HasValue),
                    UnassignedCount = group.Count(ticket =>
                        IsOpenStatus(ticket.Status) && !ticket.AssignedUserId.HasValue)
                })
                .OrderBy(row => row.CompanyName)
                .ThenBy(row => row.ProductName)
                .ToList();

            return View(model);
        }

        [Authorize(Roles = $"{RoleNames.SystemAdmin},{RoleNames.ProductManager}")]
        public async Task<IActionResult> SpecialistDistribution(
            string? specialist,
            string? product)
        {
            var scopedTickets = await GetScopedTicketsAsync();
            var products = scopedTickets
                .Select(ticket => ticket.Product?.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .OrderBy(name => name)
                .Cast<string>()
                .ToList();

            var tickets = scopedTickets.ToList();
            if (!string.IsNullOrWhiteSpace(product))
                tickets = tickets.Where(ticket => ticket.Product?.Name == product).ToList();
            if (!string.IsNullOrWhiteSpace(specialist))
            {
                var normalizedSpecialist = specialist.Trim();
                tickets = tickets.Where(ticket =>
                    ticket.AssignedUser != null &&
                    ($"{ticket.AssignedUser.FirstName} {ticket.AssignedUser.LastName}")
                        .Contains(normalizedSpecialist, StringComparison.CurrentCultureIgnoreCase))
                    .ToList();
            }

            tickets = tickets
                .Where(ticket => ticket.Status != TicketStatus.Cancelled)
                .ToList();

            var reportRows = await BuildReportRowsAsync(tickets);
            var reportRowMap = reportRows.ToDictionary(row => row.TicketId);
            var breachedIds = reportRows
                .Where(row => row.SlaState == "breached")
                .Select(row => row.TicketId)
                .ToHashSet();

            var distributionTickets = tickets
                .Where(ticket => ticket.AssignedUserId.HasValue)
                .ToList();

            var rows = distributionTickets
                .GroupBy(ticket => new
                {
                    ticket.AssignedUserId,
                    SpecialistName = ticket.AssignedUser == null
                        ? "Atanmamış"
                        : ticket.AssignedUser.FirstName + " " + ticket.AssignedUser.LastName,
                    ProductName = ticket.Product?.Name ?? "-"
                })
                .Select(group =>
                {
                    var groupReportRows = group
                        .Where(ticket => reportRowMap.ContainsKey(ticket.Id))
                        .Select(ticket => reportRowMap[ticket.Id])
                        .ToList();
                    var completedResponses = groupReportRows
                        .Where(row => row.IsResponseCompleted)
                        .ToList();
                    var completedResolutions = groupReportRows
                        .Where(row => row.IsResolutionCompleted)
                        .ToList();
                    var slaCompleted = groupReportRows
                        .Where(row => row.IsResponseCompleted && row.IsResolutionCompleted)
                        .ToList();
                    var hasSpecialist = group.Key.AssignedUserId.HasValue;

                    return new SpecialistDistributionRowViewModel
                    {
                        SpecialistId = group.Key.AssignedUserId,
                        SpecialistName = group.Key.SpecialistName,
                        ProductName = group.Key.ProductName,
                        TotalCount = group.Count(),
                        OpenCount = group.Count(ticket => IsOpenStatus(ticket.Status)),
                        InReviewCount = group.Count(ticket => ticket.Status == TicketStatus.InReview),
                        WaitingCustomerCount = group.Count(ticket => ticket.Status == TicketStatus.WaitingCustomer),
                        ResolvedCount = group.Count(ticket => ticket.Status == TicketStatus.Resolved),
                        ClosedCount = group.Count(ticket => ticket.Status == TicketStatus.Closed),
                        SlaBreachedCount = group.Count(ticket => breachedIds.Contains(ticket.Id)),
                        AverageResponseMinutes = hasSpecialist && completedResponses.Count > 0
                            ? completedResponses.Average(row => row.ResponseElapsedMinutes)
                            : null,
                        AverageResolutionMinutes = hasSpecialist && completedResolutions.Count > 0
                            ? completedResolutions.Average(row => row.ResolutionElapsedMinutes)
                            : null,
                        SlaSuccessRate = hasSpecialist && slaCompleted.Count > 0
                            ? 100.0 * slaCompleted.Count(row => !row.IsResponseBreached && !row.IsResolutionBreached) / slaCompleted.Count
                            : null
                    };
                })
                .OrderBy(row => row.SpecialistId.HasValue ? 0 : 1)
                .ThenBy(row => row.SpecialistName)
                .ThenBy(row => row.ProductName)
                .ToList();

            return View(new SpecialistDistributionReportViewModel
            {
                Rows = rows,
                Products = products,
                TotalTickets = tickets.Count(ticket => ticket.AssignedUserId.HasValue),
                AssignedTickets = tickets.Count(ticket =>
                    ticket.AssignedUserId.HasValue && IsOpenStatus(ticket.Status)),
                CompletedTickets = tickets.Count(ticket =>
                    ticket.AssignedUserId.HasValue &&
                    (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed))
            });
        }

        private static bool IsOpenStatus(string status)
        {
            return status == TicketStatus.New ||
                status == TicketStatus.SupportQueue ||
                status == TicketStatus.Assigned ||
                status == TicketStatus.InReview ||
                status == TicketStatus.WaitingCustomer;
        }

        private async Task<List<int>> GetManagedProductIdsAsync()
        {
            if (!User.IsInRole(RoleNames.ProductManager))
            {
                return new List<int>();
            }

            var userProducts = await _userProductService.GetAllAsync();

            return userProducts
                .Where(up => up.UserId == CurrentUserId && up.IsActive && up.IsProductManager)
                .Select(up => up.ProductId)
                .Distinct()
                .ToList();
        }

        private async Task<List<Ticket>> GetScopedTicketsAsync()
        {
            var tickets = await _ticketService.GetAllAsync();

            if (User.IsInRole(RoleNames.ProductManager))
            {
                var managedProductIds = await GetManagedProductIdsAsync();
                tickets = tickets.Where(t => managedProductIds.Contains(t.ProductId)).ToList();
            }
            else if (User.IsInRole(RoleNames.SupportSpecialist))
            {
                tickets = tickets.Where(t => t.AssignedUserId == CurrentUserId).ToList();
            }
            else if (User.IsInRole(RoleNames.CompanyManager))
            {
                tickets = tickets.Where(t => t.TenantId == CurrentTenantId).ToList();
            }
            else if (User.IsInRole(RoleNames.CompanyUser))
            {
                tickets = tickets.Where(t => t.TenantId == CurrentTenantId && t.CreatedByUserId == CurrentUserId).ToList();
            }

            return tickets;
        }

        private async Task<List<ReportTicketRowViewModel>> BuildReportRowsAsync(List<Ticket> tickets)
        {
            var pauses = await _slaPauseService.GetByTicketIdsAsync(tickets.Select(t => t.Id));
            var pauseMap = pauses.GroupBy(p => p.TicketId)
                .ToDictionary(g => g.Key, g => g.Select(p => (p.StartDate, p.EndDate)).ToList());
            var rows = new List<ReportTicketRowViewModel>();

            foreach (var ticket in tickets.OrderByDescending(t => t.CreatedDate))
            {
                if (ticket.SlaLevel == null || ticket.Tenant == null)
                    continue;

                pauseMap.TryGetValue(ticket.Id, out var ticketPauses);
                var tracking = SlaCalculator.CalculateTracking(ticket, ticket.SlaLevel, ticket.Tenant.SLACalculationMethod, ticketPauses);
                var terminal = ticket.Status is TicketStatus.Resolved or TicketStatus.Closed or TicketStatus.Cancelled;
                var breached = tracking.IsResponseBreached || tracking.IsResolutionBreached;
                var warning = !terminal && !breached &&
                    ((!tracking.IsResponseCompleted && tracking.ResponseRemaining.TotalMinutes <= ticket.SlaLevel.ResponseTargetMinutes * 0.25) ||
                     (!tracking.IsResolutionCompleted && tracking.ResolutionRemaining.TotalMinutes <= ticket.SlaLevel.ResolutionTargetMinutes * 0.25));
                var violationType = tracking.IsResponseBreached && tracking.IsResolutionBreached ? "İkisi birden" :
                    tracking.IsResponseBreached ? "İlk müdahale" : tracking.IsResolutionBreached ? "Çözüm" : "-";
                var delays = new List<TimeSpan>();
                if (tracking.IsResponseBreached) delays.Add(tracking.ResponseRemaining);
                if (tracking.IsResolutionBreached) delays.Add(tracking.ResolutionRemaining);

                rows.Add(new ReportTicketRowViewModel
                {
                    TicketId = ticket.Id,
                    Title = ticket.Title,
                    TenantName = ticket.Tenant.CompanyName,
                    ProductName = ticket.Product?.Name ?? "-",
                    CategoryName = ticket.Category?.Name ?? "-",
                    Priority = ticket.Priority,
                    SlaLevelName = ticket.SlaLevel.Name,
                    Status = ticket.Status,
                    SlaState = breached ? "breached" : terminal ? "completed" : warning ? "warning" : "active",
                    SlaStateText = breached ? "SLA ihlali" : terminal ? "Tamamlandı" : warning ? "Kritik süre" : "Devam ediyor",
                    ViolationType = violationType,
                    DelayText = delays.Count == 0 ? "-" : SlaDurationFormatter.FormatDuration(delays.OrderBy(d => d).First()),
                    AssignedUserName = ticket.AssignedUser == null ? "Atanmadı" : $"{ticket.AssignedUser.FirstName} {ticket.AssignedUser.LastName}",
                    CreatedDate = ticket.CreatedDate,
                    CompletionDate = ticket.ClosedDate ?? ticket.ResolvedDate,
                    IsResponseCompleted = tracking.IsResponseCompleted,
                    IsResolutionCompleted = tracking.IsResolutionCompleted,
                    IsResponseBreached = tracking.IsResponseBreached,
                    IsResolutionBreached = tracking.IsResolutionBreached,
                    ResponseElapsedMinutes = tracking.ResponseElapsed.TotalMinutes,
                    ResolutionElapsedMinutes = tracking.ResolutionElapsed.TotalMinutes
                });
            }

            return rows;
        }

        private async Task<List<Ticket>> GetSlaBreachedTicketsAsync(List<Ticket> tickets)
        {
            var slaLevels = await _slaLevelService.GetAllAsync();
            var slaLevelMap = slaLevels.ToDictionary(s => s.Id);

            var breached = new List<Ticket>();

            foreach (var ticket in tickets)
            {
                SlaLevel? slaLevel = ticket.SlaLevel;

                if (slaLevel == null)
                {
                    if (!slaLevelMap.TryGetValue(ticket.SlaLevelId, out var level))
                    {
                        continue;
                    }

                    slaLevel = level;
                }

                var pauses = (await _slaPauseService.GetByTicketIdAsync(ticket.Id))
                    .Select(p => (p.StartDate, p.EndDate))
                    .ToList();

                var calculationMethod = ticket.Tenant?.SLACalculationMethod
                    ?? SlaCalculationMethod.WorkingHours;

                var tracking = SlaCalculator.CalculateTracking(
                    ticket,
                    slaLevel,
                    calculationMethod,
                    pauses);

                if (tracking.IsResponseBreached || tracking.IsResolutionBreached)
                {
                    breached.Add(ticket);
                }
            }

            return breached;
        }
    }
}
