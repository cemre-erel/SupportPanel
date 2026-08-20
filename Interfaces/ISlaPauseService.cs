using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Interfaces
{
    public interface ISlaPauseService
    {
        Task<List<SlaPause>> GetByTicketIdAsync(int ticketId);
        Task<List<SlaPause>> GetByTicketIdsAsync(IEnumerable<int> ticketIds);
        Task StartPauseAsync(int ticketId);
        Task StopPauseAsync(int ticketId);
    }
}
