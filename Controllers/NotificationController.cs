using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPanel.Constants;
using SupportPanel.Interfaces;
using SupportPanel.Models;
using SupportPanel.Services;

namespace SupportPanel.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ITicketService _ticketService;
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;

        private int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        public NotificationController(
            INotificationService notificationService,
            ITicketService ticketService,
            IUserService userService,
            IRoleService roleService)
        {
            _notificationService = notificationService;
            _ticketService = ticketService;
            _userService = userService;
            _roleService = roleService;
        }

        public async Task<IActionResult> Index()
        {
            var notifications = await _notificationService.GetByUserAsync(CurrentUserId);

            var titles = notifications
                .Where(n => n.TicketId.HasValue && n.Ticket != null)
                .GroupBy(n => n.TicketId!.Value)
                .ToDictionary(g => g.Key, g => g.First().Ticket!.Title);

            foreach (var ticketId in notifications
                .Where(n => n.TicketId.HasValue && n.Ticket == null)
                .Select(n => n.TicketId!.Value)
                .Distinct())
            {
                var ticket = await _ticketService.GetByIdAsync(ticketId);

                if (ticket != null)
                {
                    titles[ticketId] = ticket.Title;
                }
            }

            ViewBag.TicketTitles = titles;

            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkRead(int id)
        {
            await _notificationService.MarkReadAsync(id, CurrentUserId);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            await _notificationService.MarkAllReadAsync(CurrentUserId);
            TempData["Success"] = "Tüm bildirimler okundu olarak işaretlendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.SystemAdmin)]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View(new SendNotificationViewModel());
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.SystemAdmin)]
        public async Task<IActionResult> Create(SendNotificationViewModel model)
        {
            if (model.RecipientType == "Rol")
            {
                if (string.IsNullOrWhiteSpace(model.RoleName))
                {
                    ModelState.AddModelError(nameof(model.RoleName), "Rol seçiniz.");
                }
            }
            else if (!model.UserId.HasValue)
            {
                ModelState.AddModelError(nameof(model.UserId), "Kullanıcı seçiniz.");
            }

            if (ModelState.IsValid)
            {
                if (model.RecipientType == "Rol")
                {
                    await _notificationService.SendToRoleAsync(model.RoleName!, model.Message);
                    TempData["Success"] = $"Bildirim, {model.RoleName} rolündeki kullanıcılara gönderildi.";
                }
                else
                {
                    await _notificationService.SendToUserAsync(model.UserId!.Value, model.Message);
                    TempData["Success"] = "Bildirim gönderildi.";
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownsAsync();
            return View(model);
        }

        private async Task LoadDropdownsAsync()
        {
            var users = await _userService.GetAllAsync();
            ViewBag.Users = users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();
            ViewBag.Roles = await _roleService.GetAllAsync();
        }
    }
}
