using SupportPanel.Models;

namespace SupportPanel.Services
{
    public interface ITicketCommentService
    {
        Task<List<TicketComment>> GetByTicketIdAsync(int ticketId);

        Task AddAsync(TicketComment comment);
    }
}