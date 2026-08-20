using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Services
{
    public class TicketAttachmentService : ITicketAttachmentService
    {
        private readonly ITicketAttachmentRepository _repository;

        public TicketAttachmentService(ITicketAttachmentRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<TicketAttachment>> GetByTicketIdAsync(int ticketId)
        {
            return await _repository.GetByTicketIdAsync(ticketId);
        }

        public async Task AddAsync(TicketAttachment attachment)
        {
            await _repository.AddAsync(attachment);
        }
    }
}