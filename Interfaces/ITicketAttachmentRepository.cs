using SupportPanel.Models;

namespace SupportPanel.Interfaces
{
    public interface ITicketAttachmentRepository
    {
        Task<List<TicketAttachment>> GetByTicketIdAsync(int ticketId);

        Task AddAsync(TicketAttachment attachment);
    }
}