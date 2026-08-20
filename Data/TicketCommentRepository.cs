using Microsoft.EntityFrameworkCore;
using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Data
{
    public class TicketCommentRepository : ITicketCommentRepository
    {
        private readonly AppDbContext _context;

        public TicketCommentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TicketComment>> GetByTicketIdAsync(int ticketId)
        {
            return await _context.TicketComments
                .Include(c => c.User)
                .Where(c => c.TicketId == ticketId)
                .OrderBy(c => c.CreatedDate)
                .ToListAsync();
        }

        public async Task AddAsync(TicketComment comment)
        {
            _context.TicketComments.Add(comment);
            await _context.SaveChangesAsync();
        }
    }
}