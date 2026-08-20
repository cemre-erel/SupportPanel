using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Services
{
    public class SlaPauseService : ISlaPauseService
    {
        private readonly ISlaPauseRepository _repository;

        public SlaPauseService(ISlaPauseRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<SlaPause>> GetByTicketIdAsync(int ticketId)
        {
            return await _repository.GetByTicketIdAsync(ticketId);
        }

        public async Task<List<SlaPause>> GetByTicketIdsAsync(IEnumerable<int> ticketIds)
        {
            return await _repository.GetByTicketIdsAsync(ticketIds);
        }

        public async Task StartPauseAsync(int ticketId)
        {
            await _repository.AddAsync(new SlaPause
            {
                TicketId = ticketId,
                StartDate = DateTime.Now,
                EndDate = null
            });
        }

        public async Task StopPauseAsync(int ticketId)
        {
            await _repository.CloseOpenPausesAsync(ticketId);
        }
    }
}
