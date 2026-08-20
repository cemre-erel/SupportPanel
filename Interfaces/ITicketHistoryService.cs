using SupportPanel.Models;

namespace SupportPanel.Interfaces
{
    public interface ITicketHistoryService
    {
        Task AddAsync(TicketHistory history);

        Task<List<TicketHistory>> GetByTicketIdAsync(int ticketId);
    }
}