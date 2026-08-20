using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SupportPanel.Constants;
using SupportPanel.Models;
using SupportPanel.Services;

namespace SupportPanel.Controllers
{
    [Authorize(Roles = $"{RoleNames.SystemAdmin},{RoleNames.ProductManager}")]
    public class ProductController : Controller
    {
        private readonly IProductService _service;
        private readonly IUserProductService _userProductService;

        public ProductController(IProductService service, IUserProductService userProductService)
        {
            _service = service;
            _userProductService = userProductService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _service.GetAllAsync();
            if (User.IsInRole(RoleNames.ProductManager))
            {
                var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
                var managedIds = (await _userProductService.GetAllAsync())
                    .Where(item => item.UserId == userId && item.IsActive && item.IsProductManager)
                    .Select(item => item.ProductId)
                    .ToHashSet();
                products = products.Where(product => managedIds.Contains(product.Id)).ToList();
            }
            return View(products);
        }

        [Authorize(Roles = RoleNames.SystemAdmin)]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.SystemAdmin)]
        public async Task<IActionResult> Create(
            [Bind("Name", "Version", "Description", "IsActive")] Product product)
        {
            if (ModelState.IsValid)
            {
                await _service.AddAsync(product);
                TempData["Success"] = "Ürün başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.SystemAdmin)]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _service.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.SystemAdmin)]
        public async Task<IActionResult> Edit(
            [Bind("Name", "Version", "Description", "IsActive")] Product product)
        {
            if (ModelState.IsValid)
            {
                await _service.UpdateAsync(product);
                TempData["Success"] = "Ürün başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.SystemAdmin)]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _service.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = RoleNames.SystemAdmin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _service.DeleteAsync(id);
            TempData["Success"] = "Ürün silindi.";

            return RedirectToAction(nameof(Index));
        }
    }
}
