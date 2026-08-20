using SupportPanel.Models;

namespace SupportPanel.Interfaces
{
    public interface ISlaPauseRepository
    {
        Task<List<SlaPause>> GetByTicketIdAsync(int ticketId);
        Task<List<SlaPause>> GetByTicketIdsAsync(IEnumerable<int> ticketIds);
        Task AddAsync(SlaPause pause);
        Task CloseOpenPausesAsync(int ticketId);
    }
}
