using Microsoft.EntityFrameworkCore;
using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Data
{
    public class TicketAttachmentRepository : ITicketAttachmentRepository
    {
        private readonly AppDbContext _context;

        public TicketAttachmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TicketAttachment>> GetByTicketIdAsync(int ticketId)
        {
            return await _context.TicketAttachments
                .Include(a => a.UploadedByUser)
                .Where(a => a.TicketId == ticketId)
                .OrderBy(a => a.UploadedDate)
                .ToListAsync();
        }

        public async Task AddAsync(TicketAttachment attachment)
        {
            _context.TicketAttachments.Add(attachment);
            await _context.SaveChangesAsync();
        }
    }
}