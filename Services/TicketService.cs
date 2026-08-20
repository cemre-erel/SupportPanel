using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Services
{
    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _repository;

        private readonly ITicketHistoryService _ticketHistoryService;

        public TicketService(
            ITicketRepository repository,
            ITicketHistoryService ticketHistoryService)
        {
            _repository = repository;
            _ticketHistoryService = ticketHistoryService;
        }

        public async Task<List<Ticket>> GetAllAsync(int? tenantId = null, int? userId = null, string? role = null)
        {
            return await _repository.GetAllAsync(tenantId, userId, role);
        }

        public async Task<Ticket?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task AddAsync(Ticket ticket)
        {
            await _repository.AddAsync(ticket);
        }

        public async Task UpdateAsync(Ticket ticket)
        {
            await _repository.UpdateAsync(ticket);
        }

        public async Task ChangeStatusAsync(int ticketId, string newStatus, int userId)
        {
            var ticket = await _repository.GetByIdAsync(ticketId);

            if (ticket == null)
                return;

            if (ticket.Status == TicketStatus.Cancelled)
                throw new InvalidOperationException("İptal edilen talepler güncellenemez.");

            if (ticket.Status == newStatus ||
                !TicketStatus.AllowedTransitions.TryGetValue(ticket.Status, out var allowed) ||
                !allowed.Contains(newStatus))
            {
                throw new InvalidOperationException("Geçersiz durum geçişi.");
            }

            var oldStatus = ticket.Status;

            if (!ticket.SlaStartedDate.HasValue &&
                (newStatus == TicketStatus.SupportQueue ||
                 newStatus == TicketStatus.Assigned ||
                 newStatus == TicketStatus.InReview))
            {
                ticket.SlaStartedDate = DateTime.Now;
                await _repository.UpdateAsync(ticket);

                await _ticketHistoryService.AddAsync(new TicketHistory
                {
                    TicketId = ticketId,
                    UserId = userId,
                    Action = "SLA sayacı başlatıldı",
                    NewValue = ticket.SlaStartedDate.Value.ToString("dd.MM.yyyy HH:mm:ss")
                });
            }

            await _repository.ChangeStatusAsync(ticketId, newStatus);

            await _ticketHistoryService.AddAsync(new TicketHistory
            {
                TicketId = ticketId,
                UserId = userId,
                Action = "Durum değiştirildi",
                OldValue = oldStatus,
                NewValue = newStatus
            });
        }

        public async Task CancelAsync(int ticketId, int userId)
        {
            var ticket = await _repository.GetByIdAsync(ticketId);

            if (ticket == null)
                return;

            if (ticket.Status == TicketStatus.Cancelled)
                throw new InvalidOperationException("Talep zaten iptal edilmiştir.");

            if (!TicketStatus.CanCancel(ticket.Status))
                throw new InvalidOperationException("Kapatılmış talepler iptal edilemez. Gerekirse talebi yeniden açabilirsiniz.");

            var oldStatus = ticket.Status;

            await _repository.ChangeStatusAsync(ticketId, TicketStatus.Cancelled);

            await _ticketHistoryService.AddAsync(new TicketHistory
            {
                TicketId = ticketId,
                UserId = userId,
                Action = "Talep iptal edildi",
                OldValue = oldStatus,
                NewValue = TicketStatus.Cancelled
            });
        }

        public async Task ReopenAsync(int ticketId, int userId)
        {
            var ticket = await _repository.GetByIdAsync(ticketId);

            if (ticket == null)
                return;

            if (ticket.Status != TicketStatus.Closed)
                throw new InvalidOperationException("Yalnızca kapatılmış talepler yeniden açılabilir.");

            var oldStatus = ticket.Status;
            var newStatus = TicketStatus.GetReopenTarget(ticket.AssignedUserId, ticket.SlaStartedDate);

            await _repository.ChangeStatusAsync(ticketId, newStatus);

            await _ticketHistoryService.AddAsync(new TicketHistory
            {
                TicketId = ticketId,
                UserId = userId,
                Action = "Talep yeniden açıldı",
                OldValue = oldStatus,
                NewValue = newStatus
            });
        }

        public async Task AssignUserAsync(int ticketId, int userId, int assignedByUserId, string action = "Talep atandı")
        {
            var ticket = await _repository.GetByIdAsync(ticketId);

            if (ticket == null)
                return;

            var oldUser = ticket.AssignedUser?.FirstName + " " + ticket.AssignedUser?.LastName;

            await _repository.AssignUserAsync(ticketId, userId);

            var updatedTicket = await _repository.GetByIdAsync(ticketId);

            var newUser = updatedTicket?.AssignedUser?.FirstName + " " + updatedTicket?.AssignedUser?.LastName;

            await _ticketHistoryService.AddAsync(new TicketHistory
            {
                TicketId = ticketId,
                UserId = assignedByUserId,
                Action = action,
                OldValue = string.IsNullOrWhiteSpace(oldUser) ? "-" : oldUser,
                NewValue = newUser
            });
        }

        public async Task<List<Ticket>> GetByAssignedUserAsync(int userId)
        {
            return await _repository.GetByAssignedUserAsync(userId);
        }

        public async Task<List<Ticket>> GetByTenantAsync(int tenantId)
        {
            return await _repository.GetByTenantAsync(tenantId);
        }
    }
}
