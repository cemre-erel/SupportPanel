using Microsoft.EntityFrameworkCore;
using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Data
{
    public class TicketRepository : ITicketRepository
    {
        private readonly AppDbContext _context;

        public TicketRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Ticket>> GetAllAsync(int? tenantId = null, int? userId = null, string? role = null)
        {
            var query = _context.Tickets
                .IgnoreQueryFilters()
                .Include(t => t.Tenant)
                .Include(t => t.Product)
                .Include(t => t.Category)
                .Include(t => t.AssignedUser)
                .Include(t => t.CreatedByUser)
                .Include(t => t.SlaLevel)
                .AsQueryable();

            if (role == Constants.RoleNames.CompanyUser ||
                role == Constants.RoleNames.CompanyManager)
            {
                if (tenantId.HasValue)
                {
                    query = query.Where(t => t.TenantId == tenantId.Value);
                }
            }
            else if (role == Constants.RoleNames.SupportSpecialist)
            {
                if (userId.HasValue)
                {
                    query = query.Where(t => t.AssignedUserId == userId.Value);
                }
            }
            else if (role == Constants.RoleNames.ProductManager)
            {
                if (userId.HasValue)
                {
                    var managedProductIds = await _context.UserProducts
                        .Where(up => up.UserId == userId.Value && up.IsActive && up.IsProductManager)
                        .Select(up => up.ProductId)
                        .Distinct()
                        .ToListAsync();

                    query = query.Where(t =>
                        managedProductIds.Contains(t.ProductId) &&
                        t.Status != TicketStatus.New);
                }
            }
            else if (tenantId.HasValue)
            {
                query = query.Where(t => t.TenantId == tenantId.Value);
            }

            return await query
                .OrderByDescending(t => t.CreatedDate)
                .ThenByDescending(t => t.Id)
                .ToListAsync();
        }

        public async Task<Ticket?> GetByIdAsync(int id)
        {
            return await _context.Tickets
                .Include(t => t.Tenant)
                .Include(t => t.Product)
                .Include(t => t.Category)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedUser)
                .Include(t => t.SlaLevel)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task AddAsync(Ticket ticket)
        {
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Ticket ticket)
        {
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();
        }

        public async Task ChangeStatusAsync(int ticketId, string newStatus)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);

            if (ticket == null)
            {
                return;
            }

            var previousStatus = ticket.Status;
            var statusChangedAt = DateTime.Now;

            ticket.Status = newStatus;

            if (newStatus == TicketStatus.Resolved)
            {
                ticket.ResolvedDate = statusChangedAt;
            }

            if (previousStatus == TicketStatus.Resolved &&
                newStatus == TicketStatus.InReview)
            {
                ticket.ResolvedDate = null;
            }

            if (newStatus == TicketStatus.Closed)
            {
                ticket.ClosedDate = statusChangedAt;
            }

            if (newStatus == TicketStatus.Cancelled)
            {
                ticket.CancelledDate = statusChangedAt;
            }

            if (previousStatus == TicketStatus.Closed && newStatus != TicketStatus.Closed)
            {
                var closedAt = ticket.ClosedDate ?? statusChangedAt;
                ticket.ClosedDate = null;
                ticket.ResolvedDate = null;
                _context.SlaPauses.Add(new SlaPause
                {
                    TicketId = ticketId,
                    StartDate = closedAt,
                    EndDate = statusChangedAt
                });
            }

            if (newStatus == TicketStatus.WaitingCustomer)
            {
                _context.SlaPauses.Add(new SlaPause
                {
                    TicketId = ticketId,
                    StartDate = statusChangedAt,
                    EndDate = null
                });
            }

            if (previousStatus == TicketStatus.WaitingCustomer &&
                newStatus != TicketStatus.WaitingCustomer)
            {
                var openPauses = await _context.SlaPauses
                    .Where(p => p.TicketId == ticketId && p.EndDate == null)
                    .ToListAsync();

                foreach (var pause in openPauses)
                {
                    pause.EndDate = statusChangedAt;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task AssignUserAsync(int ticketId, int userId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);

            if (ticket == null)
                return;

            ticket.AssignedUserId = userId;

            await _context.SaveChangesAsync();
        }

        public async Task<List<Ticket>> GetByAssignedUserAsync(int userId)
        {
            return await _context.Tickets
                .Include(t => t.Tenant)
                .Include(t => t.Product)
                .Include(t => t.Category)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedUser)
                .Include(t => t.SlaLevel)
                .Where(t => t.AssignedUserId == userId)
                .ToListAsync();
        }

        public async Task<List<Ticket>> GetByTenantAsync(int tenantId)
        {
            return await _context.Tickets
                .Include(t => t.Tenant)
                .Include(t => t.Product)
                .Include(t => t.Category)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedUser)
                .Include(t => t.SlaLevel)
                .Where(t => t.TenantId == tenantId)
                .OrderByDescending(t => t.CreatedDate)
                .ThenByDescending(t => t.Id)
                .ToListAsync();
        }
    }
}
