using SupportPanel.Models;

namespace SupportPanel.Services
{
    public interface ITicketAttachmentService
    {
        Task<List<TicketAttachment>> GetByTicketIdAsync(int ticketId);

        Task AddAsync(TicketAttachment attachment);
    }
}