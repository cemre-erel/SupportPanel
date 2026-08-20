using SupportPanel.Models;

namespace SupportPanel.Interfaces
{
    public interface ITicketCommentRepository
    {
        Task<List<TicketComment>> GetByTicketIdAsync(int ticketId);

        Task AddAsync(TicketComment comment);
    }
}