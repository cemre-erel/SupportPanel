using Microsoft.EntityFrameworkCore;
using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Data
{
    public class TicketHistoryRepository : ITicketHistoryRepository
    {
        private readonly AppDbContext _context;

        public TicketHistoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TicketHistory history)
        {
            _context.TicketHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TicketHistory>> GetByTicketIdAsync(int ticketId)
        {
            return await _context.TicketHistories
                .Include(h => h.User)
                .Where(h => h.TicketId == ticketId)
                .OrderByDescending(h => h.ActionDate)
                .ToListAsync();
        }
    }
}