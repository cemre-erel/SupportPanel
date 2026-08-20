using SupportPanel.Models;

namespace SupportPanel.Interfaces
{
    public interface INotificationRepository
    {
        Task<List<Notification>> GetByUserAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task AddAsync(Notification notification);
        Task MarkReadAsync(int id, int userId);
        Task MarkAllReadAsync(int userId);
        Task<List<int>> GetUserIdsByRoleAsync(string roleName);
    }
}
