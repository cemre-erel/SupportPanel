using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPanel.Constants;
using SupportPanel.Interfaces;
using SupportPanel.Models;
using SupportPanel.Services;

namespace SupportPanel.Controllers
{
    [Authorize(Roles = $"{RoleNames.SystemAdmin}")]
    public class SlaLevelController : Controller
    {
        private readonly ISlaLevelService _service;

        private readonly ITenantService _tenantService;

        public SlaLevelController(ISlaLevelService service, ITenantService tenantService)
        {
            _service = service;
            _tenantService = tenantService;
        }

        public async Task<IActionResult> Index()
        {
            var slaLevels = await _service.GetAllAsync();
            return View(slaLevels);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Tenants = await _tenantService.GetAllAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [Bind("Name", "TenantId", "IsActive", "ResponseTargetMinutes", "ResolutionTargetMinutes")] SlaLevel slaLevel)
        {
            if (ModelState.IsValid)
            {
                await _service.AddAsync(slaLevel);
                TempData["Success"] = "SLA seviyesi başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Tenants = await _tenantService.GetAllAsync();
            return View(slaLevel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var slaLevel = await _service.GetByIdAsync(id);

            if (slaLevel == null)
            {
                return NotFound();
            }

            ViewBag.Tenants = await _tenantService.GetAllAsync();
            return View(slaLevel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(
            [Bind("Id", "Name", "TenantId", "IsActive", "ResponseTargetMinutes", "ResolutionTargetMinutes")] SlaLevel slaLevel)
        {
            if (ModelState.IsValid)
            {
                await _service.UpdateAsync(slaLevel);
                TempData["Success"] = "SLA seviyesi başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Tenants = await _tenantService.GetAllAsync();
            return View(slaLevel);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var slaLevel = await _service.GetByIdAsync(id);

            if (slaLevel == null)
            {
                return NotFound();
            }

            return View(slaLevel);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _service.DeleteAsync(id);
            TempData["Success"] = "SLA seviyesi silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
