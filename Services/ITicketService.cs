using SupportPanel.Models;

namespace SupportPanel.Services
{
    public interface ITicketService
    {
        Task<List<Ticket>> GetAllAsync(int? tenantId = null, int? userId = null, string? role = null);
        Task<Ticket?> GetByIdAsync(int id);
        Task AddAsync(Ticket ticket);
        Task UpdateAsync(Ticket ticket);
        Task AssignUserAsync(int ticketId, int userId, int assignedByUserId, string action = "Talep atandı");
        Task ChangeStatusAsync(int ticketId, string newStatus, int userId);
        Task CancelAsync(int ticketId, int userId);
        Task ReopenAsync(int ticketId, int userId);
        Task<List<Ticket>> GetByAssignedUserAsync(int userId);
        Task<List<Ticket>> GetByTenantAsync(int tenantId);
    }
}
