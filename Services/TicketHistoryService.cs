using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Services
{
    public class TicketHistoryService : ITicketHistoryService
    {
        private readonly ITicketHistoryRepository _ticketHistoryRepository;

        public TicketHistoryService(ITicketHistoryRepository ticketHistoryRepository)
        {
            _ticketHistoryRepository = ticketHistoryRepository;
        }

        public async Task AddAsync(TicketHistory history)
        {
            await _ticketHistoryRepository.AddAsync(history);
        }

        public async Task<List<TicketHistory>> GetByTicketIdAsync(int ticketId)
        {
            return await _ticketHistoryRepository.GetByTicketIdAsync(ticketId);
        }
    }
}