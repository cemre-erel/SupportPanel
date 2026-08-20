using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Services
{
    public class TicketCommentService : ITicketCommentService
    {
        private readonly ITicketCommentRepository _repository;

        public TicketCommentService(ITicketCommentRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<TicketComment>> GetByTicketIdAsync(int ticketId)
        {
            return await _repository.GetByTicketIdAsync(ticketId);
        }

        public async Task AddAsync(TicketComment comment)
        {
            await _repository.AddAsync(comment);
        }
    }
}