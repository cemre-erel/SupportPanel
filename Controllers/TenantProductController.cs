using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SupportPanel.Models;
using SupportPanel.Services;

namespace SupportPanel.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = SupportPanel.Constants.RoleNames.SystemAdmin)]
    public class TenantProductController : Controller
    {
        private readonly ITenantProductService _tenantProductService;
        private readonly ITenantService _tenantService;
        private readonly IProductService _productService;

        public TenantProductController(
            ITenantProductService tenantProductService,
            ITenantService tenantService,
            IProductService productService)
        {
            _tenantProductService = tenantProductService;
            _tenantService = tenantService;
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _tenantProductService.GetAllAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Tenants = new SelectList(
                await _tenantService.GetActiveAsync(),
                "Id",
                "CompanyName");

            ViewBag.Products = new SelectList(
                (await _productService.GetAllAsync()).Where(product => product.IsActive),
                "Id",
                "Name");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(TenantProduct tenantProduct)
        {
            var tenant = await _tenantService.GetByIdAsync(tenantProduct.TenantId);
            var product = await _productService.GetByIdAsync(tenantProduct.ProductId);
            if (tenant == null || !tenant.IsActive)
                ModelState.AddModelError(nameof(TenantProduct.TenantId), "Geçerli ve aktif bir firma seçiniz.");
            if (product == null || !product.IsActive)
                ModelState.AddModelError(nameof(TenantProduct.ProductId), "Geçerli ve aktif bir ürün seçiniz.");

            var existing = (await _tenantProductService.GetAllAsync())
                .FirstOrDefault(item => item.TenantId == tenantProduct.TenantId &&
                                        item.ProductId == tenantProduct.ProductId);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(TenantProduct.ProductId), existing.IsActive
                    ? "Bu firma ve ürün zaten eşleştirilmiş."
                    : "Bu eşleştirme daha önce kaldırılmış. Listeden yeniden aktifleştirebilirsiniz.");
            }

            if (ModelState.IsValid)
            {
                await _tenantProductService.AddAsync(tenantProduct);
                TempData["Success"] = "Eşleştirme başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Tenants = new SelectList(
                await _tenantService.GetActiveAsync(),
                "Id",
                "CompanyName");

            ViewBag.Products = new SelectList(
                (await _productService.GetAllAsync()).Where(product => product.IsActive),
                "Id",
                "Name");

            return View(tenantProduct);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var item = await _tenantProductService.GetByIdAsync(id);
            if (item == null) return NotFound();

            item.IsActive = false;
            await _tenantProductService.UpdateAsync(item);
            TempData["Success"] = "Firma–ürün eşleşmesi pasif yapıldı.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var item = await _tenantProductService.GetByIdAsync(id);
            if (item == null) return NotFound();

            var tenant = await _tenantService.GetByIdAsync(item.TenantId);
            var product = await _productService.GetByIdAsync(item.ProductId);
            if (tenant == null || !tenant.IsActive || product == null || !product.IsActive)
            {
                TempData["Error"] = "Eşleştirme için firma ve ürün aktif olmalıdır.";
                return RedirectToAction(nameof(Index));
            }

            item.IsActive = true;
            await _tenantProductService.UpdateAsync(item);
            TempData["Success"] = "Firma–ürün eşleştirmesi yeniden aktifleştirildi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
