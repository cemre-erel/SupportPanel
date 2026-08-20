using SupportPanel.Models;

namespace SupportPanel.Interfaces
{
    public interface ITicketHistoryRepository
    {
        Task AddAsync(TicketHistory history);

        Task<List<TicketHistory>> GetByTicketIdAsync(int ticketId);
    }
}