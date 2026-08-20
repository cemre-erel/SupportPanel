using SupportPanel.Models;

namespace SupportPanel.Interfaces
{
    public interface INotificationService
    {
        Task<List<Notification>> GetByUserAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task SendToUserAsync(int userId, string message, int? ticketId = null);
        Task SendToUsersAsync(IEnumerable<int> userIds, string message, int? ticketId = null);
        Task SendToRoleAsync(string roleName, string message, int? ticketId = null);
        Task MarkReadAsync(int id, int userId);
        Task MarkAllReadAsync(int userId);
    }
}
