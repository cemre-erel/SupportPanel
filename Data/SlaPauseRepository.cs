using Microsoft.EntityFrameworkCore;
using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Data
{
    public class SlaPauseRepository : ISlaPauseRepository
    {
        private readonly AppDbContext _context;

        public SlaPauseRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SlaPause>> GetByTicketIdAsync(int ticketId)
        {
            return await _context.SlaPauses
                .Where(p => p.TicketId == ticketId)
                .OrderBy(p => p.StartDate)
                .ToListAsync();
        }

        public async Task<List<SlaPause>> GetByTicketIdsAsync(IEnumerable<int> ticketIds)
        {
            var ids = ticketIds.Distinct().ToList();

            if (ids.Count == 0)
                return new List<SlaPause>();

            return await _context.SlaPauses
                .Where(p => ids.Contains(p.TicketId))
                .OrderBy(p => p.TicketId)
                .ThenBy(p => p.StartDate)
                .ToListAsync();
        }

        public async Task AddAsync(SlaPause pause)
        {
            _context.SlaPauses.Add(pause);
            await _context.SaveChangesAsync();
        }

        public async Task CloseOpenPausesAsync(int ticketId)
        {
            var openPauses = await _context.SlaPauses
                .Where(p => p.TicketId == ticketId && p.EndDate == null)
                .ToListAsync();

            if (openPauses.Count > 0)
            {
                foreach (var pause in openPauses)
                {
                    pause.EndDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
            }
        }
    }
}
