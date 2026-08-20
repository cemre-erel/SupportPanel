using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPanel.Constants;
using SupportPanel.Interfaces;
using SupportPanel.Models;
using SupportPanel.Services;
using SupportPanel.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace SupportPanel.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ITicketService _ticketService;

        private readonly ITenantService _tenantService;

        private readonly IUserService _userService;

        private readonly IProductService _productService;

        private int CurrentUserId => int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private int CurrentTenantId => int.TryParse(
            User.FindFirstValue("TenantId"), out var id) ? id : 0;

        public HomeController(
            ITicketService ticketService,
            ITenantService tenantService,
            IUserService userService,
            IProductService productService)
        {
            _ticketService = ticketService;
            _tenantService = tenantService;
            _userService = userService;
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            var tickets = await GetScopedTicketsAsync();

            var model = new HomeDashboardViewModel
            {
                TotalTickets = tickets.Count,
                NewTickets = tickets.Count(t => t.Status == TicketStatus.New),
                AssignedTickets = tickets.Count(t => t.Status == TicketStatus.Assigned),
                InReviewTickets = tickets.Count(t => t.Status == TicketStatus.InReview),
                WaitingCustomerTickets = tickets.Count(t => t.Status == TicketStatus.WaitingCustomer),
                ResolvedTickets = tickets.Count(t => t.Status == TicketStatus.Resolved),
                ClosedTickets = tickets.Count(t => t.Status == TicketStatus.Closed),
                CancelledTickets = tickets.Count(t => t.Status == TicketStatus.Cancelled),
                CriticalTickets = tickets.Count(t => t.Priority == "Critical"),
                TotalTenants = (await _tenantService.GetAllAsync()).Count,
                TotalUsers = (await _userService.GetAllAsync()).Count,
                TotalProducts = (await _productService.GetAllAsync()).Count,
                RecentTickets = tickets.OrderByDescending(t => t.CreatedDate).Take(5).ToList()
            };

            return View(model);
        }

        private async Task<List<Ticket>> GetScopedTicketsAsync()
        {
            if (User.IsInRole(RoleNames.CompanyUser))
            {
                return (await _ticketService.GetAllAsync(CurrentTenantId))
                    .Where(ticket => ticket.CreatedByUserId == CurrentUserId)
                    .ToList();
            }

            if (User.IsInRole(RoleNames.CompanyManager))
            {
                return await _ticketService.GetAllAsync(CurrentTenantId);
            }

            if (User.IsInRole(RoleNames.SupportSpecialist))
            {
                return await _ticketService.GetAllAsync(
                    null, CurrentUserId, RoleNames.SupportSpecialist);
            }

            if (User.IsInRole(RoleNames.ProductManager))
            {
                return await _ticketService.GetAllAsync(
                    null, CurrentUserId, RoleNames.ProductManager);
            }

            if (User.IsInRole(RoleNames.SystemAdmin))
            {
                return await _ticketService.GetAllAsync();
            }

            return new List<Ticket>();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
