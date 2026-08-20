using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportPanel.Constants;
using SupportPanel.Data;
using SupportPanel.Interfaces;
using SupportPanel.Models;
using SupportPanel.Models.ViewModels;

namespace SupportPanel.Controllers
{
    [Authorize(Roles = RoleNames.SystemAdmin)]
    public class EmailSettingsController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly IDataProtector _protector;
        private readonly IEmailSender _emailSender;

        public EmailSettingsController(
            AppDbContext dbContext,
            IDataProtectionProvider dataProtectionProvider,
            IEmailSender emailSender)
        {
            _dbContext = dbContext;
            _protector = dataProtectionProvider.CreateProtector("SupportPanel.EmailSettings.Password.v1");
            _emailSender = emailSender;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var setting = await _dbContext.EmailSettings.AsNoTracking().FirstOrDefaultAsync();
            return View(ToViewModel(setting));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(EmailSettingsViewModel model)
        {
            ValidateEnabledSettings(model);
            if (!ModelState.IsValid)
            {
                model.HasSavedPassword = await _dbContext.EmailSettings.AnyAsync(x => x.ProtectedPassword != null);
                return View("Index", model);
            }

            var setting = await _dbContext.EmailSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new EmailSetting();
                _dbContext.EmailSettings.Add(setting);
            }

            Apply(model, setting);
            await _dbContext.SaveChangesAsync();
            TempData["Success"] = "SMTP ayarları kaydedildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTest(string? testRecipient)
        {
            if (string.IsNullOrWhiteSpace(testRecipient) || !new EmailAddressAttribute().IsValid(testRecipient))
            {
                TempData["Error"] = "Geçerli bir test alıcısı giriniz.";
                return RedirectToAction(nameof(Index));
            }

            var setting = await _dbContext.EmailSettings.AsNoTracking().FirstOrDefaultAsync();
            if (setting == null || !setting.Enabled)
            {
                TempData["Error"] = "Önce SMTP ayarlarını kaydedip e-posta gönderimini etkinleştirin.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _emailSender.SendAsync(testRecipient, "Test Alıcısı", "SupportPanel SMTP Testi",
                    "SMTP ayarlarınız başarıyla çalışıyor.");
                TempData["Success"] = $"Test e-postası {testRecipient} adresine gönderildi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Test e-postası gönderilemedi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private void ValidateEnabledSettings(EmailSettingsViewModel model)
        {
            if (!model.Enabled) return;
            if (string.IsNullOrWhiteSpace(model.Host)) ModelState.AddModelError(nameof(model.Host), "SMTP sunucusu zorunludur.");
            if (string.IsNullOrWhiteSpace(model.FromAddress)) ModelState.AddModelError(nameof(model.FromAddress), "Gönderici adresi zorunludur.");
            if (!model.HasSavedPassword && string.IsNullOrWhiteSpace(model.Password) && !string.IsNullOrWhiteSpace(model.Username))
                ModelState.AddModelError(nameof(model.Password), "SMTP şifresi veya uygulama parolası zorunludur.");
        }

        private void Apply(EmailSettingsViewModel model, EmailSetting setting)
        {
            setting.Enabled = model.Enabled;
            setting.Host = model.Host.Trim();
            setting.Port = model.Port;
            setting.EnableSsl = model.EnableSsl;
            setting.Username = model.Username?.Trim();
            setting.FromAddress = model.FromAddress.Trim();
            setting.FromName = string.IsNullOrWhiteSpace(model.FromName) ? "SupportPanel" : model.FromName.Trim();
            if (!string.IsNullOrWhiteSpace(model.Password)) setting.ProtectedPassword = _protector.Protect(model.Password);
            setting.UpdatedDate = DateTime.Now;
            setting.UpdatedByUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
        }

        private static EmailSettingsViewModel ToViewModel(EmailSetting? setting) => new()
        {
            Enabled = setting?.Enabled ?? false,
            Host = setting?.Host ?? "smtp.gmail.com",
            Port = setting?.Port ?? 587,
            EnableSsl = setting?.EnableSsl ?? true,
            Username = setting?.Username,
            FromAddress = setting?.FromAddress ?? string.Empty,
            FromName = setting?.FromName ?? "SupportPanel",
            HasSavedPassword = !string.IsNullOrWhiteSpace(setting?.ProtectedPassword)
        };
    }
}
