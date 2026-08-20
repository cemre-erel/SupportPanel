using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPanel.Constants;
using SupportPanel.Interfaces;
using SupportPanel.Models;
using SupportPanel.Services;

namespace SupportPanel.Controllers
{
    [Authorize(Roles = $"{RoleNames.SystemAdmin},{RoleNames.ProductManager},{RoleNames.CompanyManager}")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly ITenantService _tenantService;
        private readonly IRoleService _roleService;
        private readonly IProductService _productService;
        private readonly IUserProductService _userProductService;
        private readonly ITicketService _ticketService;
        private readonly ITicketHistoryService _ticketHistoryService;
        private readonly INotificationService _notificationService;
        private readonly ISlaPauseService _slaPauseService;

        private int CurrentTenantId => int.TryParse(User.FindFirstValue("TenantId"), out var id) ? id : 0;

        private int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        public UserController(
            IUserService userService,
            ITenantService tenantService,
            IRoleService roleService,
            IProductService productService,
            IUserProductService userProductService,
            ITicketService ticketService,
            ITicketHistoryService ticketHistoryService,
            INotificationService notificationService,
            ISlaPauseService slaPauseService)
        {
            _userService = userService;
            _tenantService = tenantService;
            _roleService = roleService;
            _productService = productService;
            _userProductService = userProductService;
            _ticketService = ticketService;
            _ticketHistoryService = ticketHistoryService;
            _notificationService = notificationService;
            _slaPauseService = slaPauseService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Kullanıcılar";
            var users = await _userService.GetAllAsync();

            users = users.Where(u => u.Tenant == null || u.Tenant.IsActive).ToList();

            if (User.IsInRole(RoleNames.CompanyManager))
            {
                users = users.Where(u => u.TenantId == CurrentTenantId).ToList();
            }
            else if (User.IsInRole(RoleNames.ProductManager))
            {
                users = users.Where(u => u.Role?.Name == RoleNames.SupportSpecialist).ToList();
            }

            return View(users);
        }

        [Authorize(Roles = RoleNames.SystemAdmin)]
        public async Task<IActionResult> SupportTeam()
        {
            ViewData["Title"] = "Destek Ekibi";

            var users = (await _userService.GetAllAsync())
                .Where(user => user.Role?.Name == RoleNames.ProductManager ||
                               user.Role?.Name == RoleNames.SupportSpecialist)
                .OrderByDescending(user => user.IsActive)
                .ThenBy(user => user.Role?.Name == RoleNames.ProductManager ? 0 : 1)
                .ThenBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .ToList();

            var userIds = users.Select(user => user.Id).ToHashSet();
            ViewBag.ProductAssignments = (await _userProductService.GetAllAsync())
                .Where(item => item.IsActive && item.Product != null && userIds.Contains(item.UserId))
                .GroupBy(item => item.UserId)
                .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Product!.Name).ToList());
            ViewBag.IsSupportTeam = true;

            return View("Index", users);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [Bind("FirstName", "LastName", "Username", "PasswordHash", "ConfirmPassword", "Email", "IsActive", "TenantId", "RoleId")] User user,
            List<int>? productIds,
            List<int>? specialistProductIds)
        {
            ViewData["Title"] = "Yeni Kullanıcı";
            if (User.IsInRole(RoleNames.CompanyManager))
            {
                if (CurrentTenantId == 0)
                {
                    ModelState.Remove("TenantId");
                    ModelState.AddModelError("", "Kullanıcı bir firmaya bağlı değil. Kullanıcı oluşturulamaz.");
                    await LoadDropdownsAsync();
                    return View(user);
                }

                user.TenantId = CurrentTenantId;
                ModelState.Remove("TenantId");

                var companyUserRole = await GetCompanyUserRoleAsync();
                if (companyUserRole == null || user.RoleId != companyUserRole.Id)
                {
                    ModelState.AddModelError("RoleId", "Firma yöneticisi yalnızca CompanyUser rolünde kullanıcı oluşturabilir.");
                }
            }
            else if (User.IsInRole(RoleNames.ProductManager))
            {
                await ValidateProductManagerTargetAsync(user.RoleId, productIds);
                user.TenantId = null;
                ModelState.Remove("TenantId");
            }
            else
            {
                await ApplyTenantRuleForRoleAsync(user);
                await ValidateProductAssignmentsAsync(user.RoleId, productIds);
            }

            if (ModelState.IsValid)
            {
                var existing = await _userService.GetByUsernameAsync(user.Username);
                if (existing != null)
                {
                    ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanılıyor.");
                    await LoadDropdownsAsync(selectedProductIds: productIds, selectedSpecialistProductIds: specialistProductIds);
                    return View(user);
                }

                var existingEmail = await _userService.GetByEmailAsync(user.Email);
                if (existingEmail != null)
                {
                    ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
                    await LoadDropdownsAsync(selectedProductIds: productIds, selectedSpecialistProductIds: specialistProductIds);
                    return View(user);
                }

                await _userService.AddAsync(user);
                await UpdateProductAssignmentsAsync(user.Id, user.RoleId, user.IsActive, productIds, specialistProductIds);
                TempData["Success"] = "Kullanıcı başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownsAsync(selectedProductIds: productIds, selectedSpecialistProductIds: specialistProductIds);

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? returnTo = null)
        {
            var returnToSupportTeam =
                User.IsInRole(RoleNames.SystemAdmin) && returnTo == "support-team";
            ViewBag.ReturnToSupportTeam = returnToSupportTeam;
            ViewData["Title"] = returnToSupportTeam ? "Destek Ekibi - Düzenle" : "Kullanıcı Düzenle";
            var user = await _userService.GetByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (User.IsInRole(RoleNames.CompanyManager))
            {
                if (user.TenantId != CurrentTenantId)
                {
                    TempData["Error"] = "Yalnızca kendi firmanızın kullanıcılarını düzenleyebilirsiniz.";
                    return RedirectToAction(nameof(Index));
                }

                user.TenantId = CurrentTenantId;

                var companyUserRole = await GetCompanyUserRoleAsync();
                if (companyUserRole == null || user.RoleId != companyUserRole.Id)
                {
                    TempData["Error"] = "Firma yöneticisi yalnızca CompanyUser rolündeki kullanıcıları düzenleyebilir.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else if (User.IsInRole(RoleNames.ProductManager) && user.Role?.Name != RoleNames.SupportSpecialist)
            {
                return Forbid();
            }

            await LoadDropdownsAsync(user.Id);

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(
            [Bind("Id", "FirstName", "LastName", "Username", "PasswordHash", "ConfirmPassword", "Email", "IsActive", "TenantId", "RoleId")] User user,
            List<int>? productIds,
            List<int>? specialistProductIds,
            string? returnTo = null)
        {
            var returnToSupportTeam =
                User.IsInRole(RoleNames.SystemAdmin) && returnTo == "support-team";
            ViewBag.ReturnToSupportTeam = returnToSupportTeam;
            ViewData["Title"] = returnToSupportTeam ? "Destek Ekibi - Düzenle" : "Kullanıcı Düzenle";
            var existingUser = await _userService.GetByIdAsync(user.Id);

            if (existingUser == null)
            {
                return NotFound();
            }

            var wasActive = existingUser.IsActive;
            var previousRoleName = existingUser.Role?.Name;
            var releasedManagerProductNames = wasActive && !user.IsActive && previousRoleName == RoleNames.ProductManager
                ? await GetManagedProductNamesAsync(existingUser.Id)
                : new List<string>();

            if (User.IsInRole(RoleNames.CompanyManager))
            {
                if (existingUser.TenantId != CurrentTenantId)
                {
                    TempData["Error"] = "Yalnızca kendi firmanızın kullanıcılarını düzenleyebilirsiniz.";
                    return RedirectToAction(nameof(Index));
                }

                user.TenantId = CurrentTenantId;
                ModelState.Remove("TenantId");


                var companyUserRole = await GetCompanyUserRoleAsync();
                if (companyUserRole == null ||
                    existingUser.RoleId != companyUserRole.Id ||
                    user.RoleId != companyUserRole.Id)
                {
                    ModelState.AddModelError("RoleId", "Firma yöneticisi yalnızca CompanyUser rolündeki kullanıcıları düzenleyebilir.");
                }
            }

            else if (User.IsInRole(RoleNames.ProductManager))
            {
                if (existingUser.Role?.Name != RoleNames.SupportSpecialist)
                {
                    return Forbid();
                }

                await ValidateProductManagerTargetAsync(user.RoleId, productIds);
                user.TenantId = null;
                ModelState.Remove("TenantId");
            }
            else
            {
                await ApplyTenantRuleForRoleAsync(user);
                await ValidateProductAssignmentsAsync(user.RoleId, productIds, user.Id);
            }

            // Şifre alanı boş bırakıldıysa mevcut hash korunur. Doldurulduysa düz metin olarak
            // saklanır ve doğrulamalar geçtikten sonra, kaydetmeden hemen önce hash'lenir;
            // böylece validation hatasında hash view'a geri dönmez.
            var newPassword = string.IsNullOrWhiteSpace(user.PasswordHash) ? null : user.PasswordHash;

            if (newPassword == null)
            {
                user.PasswordHash = existingUser.PasswordHash ?? "";
                ModelState.Remove("PasswordHash");
                ModelState.Remove("ConfirmPassword");
            }

            if (ModelState.IsValid)
            {
                var existing = await _userService.GetByUsernameAsync(user.Username);
                if (existing != null && existing.Id != user.Id)
                {
                    ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanılıyor.");
                    await LoadDropdownsAsync(user.Id, productIds, specialistProductIds);
                    return View(user);
                }

                var existingEmail = await _userService.GetByEmailAsync(user.Email);
                if (existingEmail != null && existingEmail.Id != user.Id)
                {
                    ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
                    await LoadDropdownsAsync(user.Id, productIds, specialistProductIds);
                    return View(user);
                }

                if (newPassword != null)
                {
                    user.PasswordHash = _userService.HashPassword(user, newPassword);
                }

                await _userService.UpdateAsync(user);
                await UpdateProductAssignmentsAsync(user.Id, user.RoleId, user.IsActive, productIds, specialistProductIds);
                if (wasActive && !user.IsActive)
                {
                    await HandleDeactivatedSupportUserAsync(user, previousRoleName, releasedManagerProductNames);
                }
                TempData["Success"] = "Kullanıcı başarıyla güncellendi.";
                return returnToSupportTeam
                    ? RedirectToAction(nameof(SupportTeam))
                    : RedirectToAction(nameof(Index));
            }

            await LoadDropdownsAsync(user.Id, productIds, specialistProductIds);

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            ViewData["Title"] = "Kullanıcı Sil";
            var user = await _userService.GetByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (User.IsInRole(RoleNames.CompanyManager) && user.TenantId != CurrentTenantId)
            {
                TempData["Error"] = "Yalnızca kendi firmanızın kullanıcılarını silebilirsiniz.";
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole(RoleNames.ProductManager) && user.Role?.Name != RoleNames.SupportSpecialist)
            {
                return Forbid();
            }

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userService.GetByIdAsync(id);

            if (User.IsInRole(RoleNames.CompanyManager))
            {
                if (user == null || user.TenantId != CurrentTenantId)
                {
                    TempData["Error"] = "Yalnızca kendi firmanızın kullanıcılarını silebilirsiniz.";
                    return RedirectToAction(nameof(Index));
                }
            }

            if (User.IsInRole(RoleNames.ProductManager) && user?.Role?.Name != RoleNames.SupportSpecialist)
            {
                return Forbid();
            }

            if (user != null && user.Id == CurrentUserId)
            {
                TempData["Error"] = "Kendi hesabınızı silemezsiniz.";
                return RedirectToAction(nameof(Index));
            }

            if (user != null)
            {
                var previousRoleName = user.Role?.Name;
                var releasedManagerProductNames = previousRoleName == RoleNames.ProductManager
                    ? await GetManagedProductNamesAsync(user.Id)
                    : new List<string>();
                user.IsActive = false;
                await _userService.UpdateAsync(user);
                await UpdateProductAssignmentsAsync(user.Id, user.RoleId, false, null, null);
                await HandleDeactivatedSupportUserAsync(user, previousRoleName, releasedManagerProductNames);
                TempData["Success"] = "Kullanıcı pasifleştirildi; geçmiş kayıtları korundu.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Activate(int id, string? returnTo = null)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (User.IsInRole(RoleNames.CompanyManager))
            {
                if (user.TenantId != CurrentTenantId || user.Role?.Name != RoleNames.CompanyUser)
                {
                    return Forbid();
                }
            }
            else if (User.IsInRole(RoleNames.ProductManager) &&
                     user.Role?.Name != RoleNames.SupportSpecialist)
            {
                return Forbid();
            }

            if (user.Tenant != null && !user.Tenant.IsActive)
            {
                TempData["Error"] = "Pasif firmaya bağlı kullanıcı aktifleştirilemez. Önce firmayı aktifleştirin.";
                return returnTo == "support-team"
                    ? RedirectToAction(nameof(SupportTeam))
                    : RedirectToAction(nameof(Index));
            }

            if (!user.IsActive)
            {
                user.IsActive = true;
                await _userService.UpdateAsync(user);
                TempData["Success"] = "Kullanıcı aktifleştirildi. Gerekliyse ürün sorumluluklarını yeniden tanımlayın.";
            }

            return returnTo == "support-team"
                ? RedirectToAction(nameof(SupportTeam))
                : RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Yeni Kullanıcı";
            await LoadDropdownsAsync();

            return View();
        }

        private async Task LoadDropdownsAsync(int? editedUserId = null, List<int>? selectedProductIds = null, List<int>? selectedSpecialistProductIds = null)
        {
            if (User.IsInRole(RoleNames.CompanyManager))
            {
                ViewBag.Tenants = new List<Tenant>();
                ViewBag.CurrentTenant = await _tenantService.GetByIdAsync(CurrentTenantId);
                var companyUserRole = await GetCompanyUserRoleAsync();
                ViewBag.Roles = companyUserRole == null
                    ? new List<Role>()
                    : new List<Role> { companyUserRole };
            }
            else if (User.IsInRole(RoleNames.ProductManager))
            {
                ViewBag.Tenants = new List<Tenant>();
                var supportRole = (await _roleService.GetAllAsync())
                    .FirstOrDefault(role => role.Name == RoleNames.SupportSpecialist);
                ViewBag.Roles = supportRole == null ? new List<Role>() : new List<Role> { supportRole };
                var managedIds = await GetManagedProductIdsAsync();
                ViewBag.Products = (await _productService.GetAllAsync())
                    .Where(product => product.IsActive && managedIds.Contains(product.Id))
                    .OrderBy(product => product.Name)
                    .ToList();
                ViewBag.SelectedProductIds = selectedProductIds ?? (editedUserId.HasValue
                    ? (await _userProductService.GetAllAsync())
                        .Where(item => item.UserId == editedUserId.Value && item.IsActive && item.IsSupportSpecialist && managedIds.Contains(item.ProductId))
                        .Select(item => item.ProductId)
                        .ToList()
                    : new List<int>());
            }
            else
            {
                ViewBag.Tenants = await _tenantService.GetActiveAsync();
                ViewBag.Roles = await _roleService.GetAllAsync();
                ViewBag.Products = (await _productService.GetAllAsync())
                    .Where(product => product.IsActive)
                    .OrderBy(product => product.Name)
                    .ToList();
                var editedUser = editedUserId.HasValue
                    ? await _userService.GetByIdAsync(editedUserId.Value)
                    : null;
                var editedUserIsSpecialist = editedUser?.Role?.Name == RoleNames.SupportSpecialist;
                ViewBag.SelectedProductIds = selectedProductIds ?? (editedUserId.HasValue
                    ? (await _userProductService.GetAllAsync())
                        .Where(item => item.UserId == editedUserId.Value && item.IsActive &&
                            (editedUserIsSpecialist ? item.IsSupportSpecialist : item.IsProductManager))
                        .Select(item => item.ProductId)
                        .ToList()
                    : new List<int>());
                ViewBag.SelectedSpecialistProductIds = selectedSpecialistProductIds ?? (editedUserId.HasValue
                    ? (await _userProductService.GetAllAsync())
                        .Where(item => item.UserId == editedUserId.Value && item.IsActive && item.IsSupportSpecialist)
                        .Select(item => item.ProductId)
                        .ToList()
                    : new List<int>());
            }
        }

        private async Task ValidateProductAssignmentsAsync(int roleId, List<int>? productIds, int? excludedUserId = null)
        {
            if (!User.IsInRole(RoleNames.SystemAdmin)) return;

            var role = (await _roleService.GetAllAsync()).FirstOrDefault(item => item.Id == roleId);
            var isProductManager = role?.Name == RoleNames.ProductManager;
            var isSupportSpecialist = role?.Name == RoleNames.SupportSpecialist;
            if (!isProductManager && !isSupportSpecialist) return;

            var selectedIds = (productIds ?? new List<int>()).Distinct().ToList();
            if (selectedIds.Count == 0)
            {
                ModelState.AddModelError(
                    "ProductIds",
                    isProductManager
                        ? "Ürün yöneticisi için en az bir ürün seçiniz."
                        : "Destek uzmanı için en az bir ürün seçiniz.");
                return;
            }

            var activeIds = (await _productService.GetAllAsync())
                .Where(product => product.IsActive)
                .Select(product => product.Id)
                .ToHashSet();
            if (selectedIds.Any(id => !activeIds.Contains(id)))
            {
                ModelState.AddModelError("ProductIds", "Yalnızca geçerli ve aktif ürünler seçilebilir.");
            }

            if (!isProductManager) return;

            var users = await _userService.GetAllAsync();
            var userNames = users.ToDictionary(
                user => user.Id,
                user => $"{user.FirstName} {user.LastName}".Trim());
            var products = (await _productService.GetAllAsync())
                .ToDictionary(product => product.Id, product => product.Name);
            var conflicts = (await _userProductService.GetAllAsync())
                .Where(item => item.IsActive &&
                    item.IsProductManager &&
                    selectedIds.Contains(item.ProductId) &&
                    (!excludedUserId.HasValue || item.UserId != excludedUserId.Value))
                .GroupBy(item => item.ProductId)
                .Select(group => new
                {
                    ProductName = products.GetValueOrDefault(group.Key, $"Ürün #{group.Key}"),
                    ManagerNames = string.Join(", ", group
                        .Select(item => userNames.GetValueOrDefault(item.UserId, $"Kullanıcı #{item.UserId}"))
                        .Distinct())
                })
                .ToList();

            foreach (var conflict in conflicts)
            {
                ModelState.AddModelError(
                    "ProductIds",
                    $"{conflict.ProductName} ürünü zaten {conflict.ManagerNames} tarafından yönetiliyor.");
            }
        }

        private async Task UpdateProductAssignmentsAsync(
            int userId,
            int roleId,
            bool userIsActive,
            List<int>? productIds,
            List<int>? specialistProductIds)
        {
            if (!User.IsInRole(RoleNames.SystemAdmin) && !User.IsInRole(RoleNames.ProductManager)) return;

            var role = (await _roleService.GetAllAsync()).FirstOrDefault(item => item.Id == roleId);
            var managerIds = role?.Name == RoleNames.ProductManager
                ? (productIds ?? new List<int>()).Distinct().ToHashSet()
                : new HashSet<int>();
            var specialistIds = role?.Name == RoleNames.SupportSpecialist
                ? (productIds ?? new List<int>()).Distinct().ToHashSet()
                : role?.Name == RoleNames.ProductManager
                    ? (specialistProductIds ?? new List<int>()).Distinct().ToHashSet()
                    : new HashSet<int>();
            if (!userIsActive)
            {
                // Pasif kullanıcı görev alamaz. Eşleşmeler silinmez; geçmiş kayıt korunurken
                // ürün yöneticiliği üzerindeki benzersiz aktif atama da serbest bırakılır.
                managerIds.Clear();
                specialistIds.Clear();
            }
            if (User.IsInRole(RoleNames.ProductManager))
            {
                specialistIds.IntersectWith(await GetManagedProductIdsAsync());
                managerIds.Clear();
            }
            var existing = (await _userProductService.GetAllAsync())
                .Where(item => item.UserId == userId)
                .ToList();
            if (!userIsActive)
            {
                foreach (var item in existing.Where(item => item.IsActive))
                {
                    item.IsActive = false;
                    await _userProductService.UpdateAsync(item);
                }
                return;
            }
            var newlyManagedProductIds = new HashSet<int>();
            if (User.IsInRole(RoleNames.ProductManager))
            {
                var managedIds = await GetManagedProductIdsAsync();
                existing = existing.Where(item => managedIds.Contains(item.ProductId)).ToList();
            }

            foreach (var item in existing)
            {
                var isManager = managerIds.Remove(item.ProductId);
                var isSpecialist = specialistIds.Remove(item.ProductId);
                var active = isManager || isSpecialist;
                if (userIsActive && isManager && (!item.IsActive || !item.IsProductManager))
                {
                    newlyManagedProductIds.Add(item.ProductId);
                }
                if (item.IsActive != active || item.IsProductManager != isManager || item.IsSupportSpecialist != isSpecialist)
                {
                    item.IsActive = active;
                    item.IsProductManager = isManager;
                    item.IsSupportSpecialist = isSpecialist;
                    await _userProductService.UpdateAsync(item);
                }
            }

            foreach (var productId in managerIds.Union(specialistIds))
            {
                if (managerIds.Contains(productId)) newlyManagedProductIds.Add(productId);
                await _userProductService.AddAsync(new UserProduct
                {
                    UserId = userId,
                    ProductId = productId,
                    IsActive = true,
                    IsProductManager = managerIds.Contains(productId),
                    IsSupportSpecialist = specialistIds.Contains(productId)
                });
            }

            if (userIsActive && newlyManagedProductIds.Count > 0)
            {
                var productNames = (await _productService.GetAllAsync())
                    .Where(product => newlyManagedProductIds.Contains(product.Id))
                    .Select(product => product.Name)
                    .OrderBy(name => name)
                    .ToList();
                await _notificationService.SendToUserAsync(
                    userId,
                    $"Ürün yöneticiliği atandı: {string.Join(", ", productNames)}. Destek havuzundaki talepleri kontrol edin.");
            }
        }

        private async Task<List<string>> GetManagedProductNamesAsync(int userId)
        {
            return (await _userProductService.GetAllAsync())
                .Where(item => item.UserId == userId && item.IsActive && item.IsProductManager && item.Product != null)
                .Select(item => item.Product!.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
        }

        private async Task HandleDeactivatedSupportUserAsync(
            User user,
            string? previousRoleName,
            IReadOnlyCollection<string> affectedProductNames)
        {
            if (previousRoleName != RoleNames.ProductManager &&
                previousRoleName != RoleNames.SupportSpecialist)
            {
                return;
            }

            var openStatuses = new HashSet<string>
            {
                TicketStatus.New,
                TicketStatus.Assigned,
                TicketStatus.InReview,
                TicketStatus.WaitingCustomer
            };
            var affectedTickets = (await _ticketService.GetByAssignedUserAsync(user.Id))
                .Where(ticket => openStatuses.Contains(ticket.Status))
                .ToList();

            foreach (var ticket in affectedTickets)
            {
                var oldStatus = ticket.Status;
                if (oldStatus == TicketStatus.WaitingCustomer)
                {
                    await _slaPauseService.StopPauseAsync(ticket.Id);
                }

                ticket.AssignedUserId = null;
                ticket.AssignedUser = null;
                ticket.Status = TicketStatus.SupportQueue;
                await _ticketService.UpdateAsync(ticket);
                await _ticketHistoryService.AddAsync(new TicketHistory
                {
                    TicketId = ticket.Id,
                    UserId = CurrentUserId,
                    Action = "Atanan kullanıcı pasifleştirildi; destek havuzuna aktarıldı",
                    OldValue = $"{user.FirstName} {user.LastName} / {TicketStatus.DisplayNames.GetValueOrDefault(oldStatus, oldStatus)}",
                    NewValue = TicketStatus.DisplayNames[TicketStatus.SupportQueue]
                });

                var managerIds = (await _userProductService.GetAllAsync())
                    .Where(item => item.ProductId == ticket.ProductId && item.IsActive && item.IsProductManager)
                    .Select(item => item.UserId)
                    .Where(id => id != user.Id)
                    .Distinct()
                    .ToList();
                if (managerIds.Count > 0)
                {
                    await _notificationService.SendToUsersAsync(
                        managerIds,
                        $"{ticket.Title} (#{ticket.Id}) talebi, atanan kullanıcı pasifleştirildiği için destek havuzuna döndü.",
                        ticket.Id);
                }
            }

            if (previousRoleName == RoleNames.ProductManager && affectedProductNames.Count > 0)
            {
                var message =
                    $"{user.FirstName} {user.LastName} pasifleştirildi. Yeni yönetici bekleyen ürünler: {string.Join(", ", affectedProductNames)}.";
                await _notificationService.SendToRoleAsync(
                    RoleNames.SystemAdmin,
                    message);
                await _notificationService.SendToRoleAsync(
                    RoleNames.ProductManager,
                    message);
            }
        }

        private async Task<Role?> GetCompanyUserRoleAsync()
        {
            return (await _roleService.GetAllAsync())
                .FirstOrDefault(role => role.Name == RoleNames.CompanyUser);
        }

        private async Task ValidateProductManagerTargetAsync(int roleId, List<int>? productIds)
        {
            var role = (await _roleService.GetAllAsync()).FirstOrDefault(item => item.Id == roleId);
            if (role?.Name != RoleNames.SupportSpecialist)
            {
                ModelState.AddModelError("RoleId", "Ürün yöneticisi yalnızca Destek Uzmanı rolünü yönetebilir.");
                return;
            }

            var selectedIds = (productIds ?? new List<int>()).Distinct().ToList();
            if (selectedIds.Count == 0)
            {
                ModelState.AddModelError("ProductIds", "Destek uzmanı için en az bir ürün seçiniz.");
                return;
            }

            var managedIds = await GetManagedProductIdsAsync();
            if (selectedIds.Any(id => !managedIds.Contains(id)))
            {
                ModelState.AddModelError("ProductIds", "Yalnızca sorumlu olduğunuz ürünleri atayabilirsiniz.");
            }
        }

        private async Task<HashSet<int>> GetManagedProductIdsAsync()
        {
            return (await _userProductService.GetAllAsync())
                .Where(item => item.UserId == CurrentUserId && item.IsActive && item.IsProductManager)
                .Select(item => item.ProductId)
                .ToHashSet();
        }

        private async Task ApplyTenantRuleForRoleAsync(User user)
        {
            var role = (await _roleService.GetAllAsync())
                .FirstOrDefault(item => item.Id == user.RoleId);

            if (role == null)
            {
                ModelState.AddModelError(nameof(SupportPanel.Models.User.RoleId), "Geçerli bir rol seçiniz.");
                return;
            }

            var isCustomerRole = role.Name == RoleNames.CompanyManager ||
                role.Name == RoleNames.CompanyUser;

            if (!isCustomerRole)
            {
                user.TenantId = null;
                ModelState.Remove(nameof(SupportPanel.Models.User.TenantId));
                return;
            }

            if (!user.TenantId.HasValue || user.TenantId.Value <= 0)
            {
                ModelState.AddModelError(nameof(SupportPanel.Models.User.TenantId), "Firma seçiniz.");
                return;
            }

            var tenant = await _tenantService.GetByIdAsync(user.TenantId.Value);
            if (tenant == null || !tenant.IsActive)
            {
                ModelState.AddModelError(nameof(SupportPanel.Models.User.TenantId), "Geçerli ve aktif bir firma seçiniz.");
            }
        }
    }
}
