using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportPanel.Constants;
using SupportPanel.Models;
using SupportPanel.Services;

namespace SupportPanel.Controllers
{
    [Authorize(Roles = RoleNames.SystemAdmin)]
    public class TenantController : Controller
    {
        private readonly ITenantService _service;

        public TenantController(ITenantService service)
        {
            _service = service;
        }

        // İç roller firmaya bağlı değildir; bu nedenle herhangi bir firmayı pasifleştirebilir.

        public async Task<IActionResult> Index()
        {
            var tenants = await _service.GetAllAsync();
            return View(tenants);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [Bind("CompanyName", "ContactName", "Email", "Phone", "SLACalculationMethod")] Tenant tenant)
        {
            tenant.CompanyName = (tenant.CompanyName ?? string.Empty).Trim();
            tenant.Email = (tenant.Email ?? string.Empty).Trim();
            tenant.Phone = (tenant.Phone ?? string.Empty).Trim();

            await AddDuplicateValidationErrorsAsync(tenant.CompanyName, tenant.Email, tenant.Phone);

            if (ModelState.IsValid)
            {
                await _service.AddAsync(tenant);
                TempData["Success"] = "Firma başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }

            return View(tenant);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var tenant = await _service.GetByIdAsync(id);

            if (tenant == null)
            {
                return NotFound();
            }

            return View(tenant);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("CompanyName", "ContactName", "Email", "Phone", "SLACalculationMethod")] Tenant form)
        {
            var tenant = await _service.GetByIdAsync(id);

            if (tenant == null)
            {
                return NotFound();
            }

            form.CompanyName = (form.CompanyName ?? string.Empty).Trim();
            form.Email = (form.Email ?? string.Empty).Trim();
            form.Phone = (form.Phone ?? string.Empty).Trim();

            await AddDuplicateValidationErrorsAsync(form.CompanyName, form.Email, form.Phone, id);

            if (!ModelState.IsValid)
            {
                form.Id = id;
                return View(form);
            }

            tenant.CompanyName = form.CompanyName;
            tenant.ContactName = form.ContactName;
            tenant.Email = form.Email;
            tenant.Phone = form.Phone;
            tenant.SLACalculationMethod = form.SLACalculationMethod;

            await _service.UpdateAsync(tenant);
            TempData["Success"] = "Firma başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));

        }

        private async Task AddDuplicateValidationErrorsAsync(
            string companyName,
            string email,
            string phone,
            int? excludedTenantId = null)
        {
            if (!string.IsNullOrWhiteSpace(companyName) &&
                await _service.CompanyNameExistsAsync(companyName, excludedTenantId))
            {
                ModelState.AddModelError(nameof(Tenant.CompanyName), "Bu firma adı zaten kullanılıyor.");
            }

            if (!string.IsNullOrWhiteSpace(email) &&
                await _service.EmailExistsAsync(email, excludedTenantId))
            {
                ModelState.AddModelError(nameof(Tenant.Email), "Bu e-posta adresi başka bir firmada kullanılıyor.");
            }

            if (!string.IsNullOrWhiteSpace(phone) &&
                await _service.PhoneExistsAsync(phone, excludedTenantId))
            {
                ModelState.AddModelError(nameof(Tenant.Phone), "Bu telefon numarası başka bir firmada kullanılıyor.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var tenant = await _service.GetByIdAsync(id);

            if (tenant == null)
            {
                return NotFound();
            }

            return View(tenant);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                TempData["Success"] = "Firma pasifleştirildi.";
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = "Firma pasifleştirilemedi. " + ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Firma pasifleştirilemedi. " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reactivate(int id)
        {
            try
            {
                await _service.ReactivateAsync(id);
                TempData["Success"] = "Firma yeniden aktifleştirildi.";
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = "Firma yeniden aktifleştirilemedi. " + ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Firma yeniden aktifleştirilemedi. " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
