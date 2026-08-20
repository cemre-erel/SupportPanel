using SupportPanel.Models;

namespace SupportPanel.Interfaces
{
    public interface ITicketRepository
    {
        Task<List<Ticket>> GetAllAsync(int? tenantId = null, int? userId = null, string? role = null);
        Task<Ticket?> GetByIdAsync(int id);
        Task AddAsync(Ticket ticket);
        Task UpdateAsync(Ticket ticket);
        Task AssignUserAsync(int ticketId, int userId);
        Task ChangeStatusAsync(int ticketId, string newStatus);
        Task<List<Ticket>> GetByAssignedUserAsync(int userId);
        Task<List<Ticket>> GetByTenantAsync(int tenantId);
    }
}