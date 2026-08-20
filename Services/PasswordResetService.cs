using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using SupportPanel.Data;
using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

        private readonly AppDbContext _dbContext;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PasswordResetService> _logger;
        private readonly PasswordHasher<User> _hasher = new();

        public PasswordResetService(
            AppDbContext dbContext,
            IEmailSender emailSender,
            ILogger<PasswordResetService> logger)
        {
            _dbContext = dbContext;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task RequestResetAsync(string email, string resetLinkBaseUrl)
        {
            var normalizedEmail = email.Trim();

            var user = await _dbContext.Users
                .IgnoreQueryFilters()
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null || !user.IsActive || user.Tenant?.IsActive == false)
            {
                _logger.LogInformation(
                    "Şifre sıfırlama talebi: {Email} için gönderilebilir kullanıcı bulunamadı.",
                    normalizedEmail);
                return;
            }

            var outstandingTokens = await _dbContext.PasswordResetTokens
                .Where(t => t.UserId == user.Id && t.UsedAtUtc == null)
                .ToListAsync();

            foreach (var old in outstandingTokens)
            {
                old.UsedAtUtc = DateTime.UtcNow;
            }

            var rawToken = GenerateRawToken();
            var token = new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = HashToken(rawToken),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.Add(TokenLifetime)
            };

            _dbContext.PasswordResetTokens.Add(token);
            await _dbContext.SaveChangesAsync();

            var resetLink = $"{resetLinkBaseUrl.TrimEnd('/')}?token={Uri.EscapeDataString(rawToken)}";
            var fullName = $"{user.FirstName} {user.LastName}".Trim();

            try
            {
                await _emailSender.SendAsync(
                    user.Email,
                    fullName,
                    "SupportPanel - Şifre Sıfırlama",
                    "Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayın:\r\n" +
                    $"{resetLink}\r\n\r\n" +
                    $"Bu bağlantı {TokenLifetime.TotalMinutes:0} dakika boyunca ve yalnızca bir kez kullanılabilir. " +
                    "Bu talebi siz oluşturmadıysanız bu e-postayı yok sayabilirsiniz.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre sıfırlama e-postası gönderilemedi. UserId: {UserId}", user.Id);
            }
        }

        public async Task<bool> IsTokenValidAsync(string rawToken)
        {
            var token = await FindTokenAsync(rawToken);
            return token is { IsValid: true };
        }

        public async Task<PasswordResetResult> ResetPasswordAsync(string rawToken, string newPassword)
        {
            var token = await FindTokenAsync(rawToken);
            if (token is not { IsValid: true })
            {
                return PasswordResetResult.InvalidOrExpiredToken;
            }

            var user = await _dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == token.UserId);

            if (user == null || !user.IsActive)
            {
                return PasswordResetResult.InvalidOrExpiredToken;
            }

            user.PasswordHash = _hasher.HashPassword(user, newPassword);
            token.UsedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return PasswordResetResult.Success;
        }

        private async Task<PasswordResetToken?> FindTokenAsync(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return null;
            }

            var hash = HashToken(rawToken);
            return await _dbContext.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash);
        }

        private static string GenerateRawToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        private static string HashToken(string rawToken)
        {
            var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes);
        }
    }
}