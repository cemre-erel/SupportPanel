using SupportPanel.Models;

namespace SupportPanel.Interfaces
{
    public interface IPasswordResetService
    {
        Task RequestResetAsync(string email, string resetLinkBaseUrl);
        Task<bool> IsTokenValidAsync(string rawToken);
        Task<PasswordResetResult> ResetPasswordAsync(string rawToken, string newPassword);
    }

    public enum PasswordResetResult
    {
        Success,
        InvalidOrExpiredToken
    }
}