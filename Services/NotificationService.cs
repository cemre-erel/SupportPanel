using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly IUserService _userService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository repository,
            IUserService userService,
            IEmailSender emailSender,
            ILogger<NotificationService> logger)
        {
            _repository = repository;
            _userService = userService;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<List<Notification>> GetByUserAsync(int userId)
        {
            return await _repository.GetByUserAsync(userId);
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _repository.GetUnreadCountAsync(userId);
        }

        public async Task SendToUserAsync(int userId, string message, int? ticketId = null)
        {
            await _repository.AddAsync(new Notification
            {
                UserId = userId,
                TicketId = ticketId,
                Message = message,
                IsRead = false,
                CreatedDate = DateTime.Now
            });

            var user = await _userService.GetByIdAsync(userId);
            if (user == null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
            {
                return;
            }

            try
            {
                var subject = ticketId.HasValue
                    ? $"SupportPanel - Talep #{ticketId.Value} bildirimi"
                    : "SupportPanel bildirimi";
                await _emailSender.SendAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}".Trim(),
                    subject,
                    message);
            }
            catch (Exception ex)
            {
                // E-posta servisindeki geçici bir sorun, asıl talep işlemini ve uygulama
                // içi bildirimi başarısız hale getirmemelidir.
                _logger.LogError(ex, "Bildirim e-postası gönderilemedi. UserId: {UserId}", userId);
            }
        }

        public async Task SendToUsersAsync(IEnumerable<int> userIds, string message, int? ticketId = null)
        {
            foreach (var userId in userIds.Distinct())
            {
                await SendToUserAsync(userId, message, ticketId);
            }
        }

        public async Task SendToRoleAsync(string roleName, string message, int? ticketId = null)
        {
            var userIds = await _repository.GetUserIdsByRoleAsync(roleName);
            await SendToUsersAsync(userIds, message, ticketId);
        }

        public async Task MarkReadAsync(int id, int userId)
        {
            await _repository.MarkReadAsync(id, userId);
        }

        public async Task MarkAllReadAsync(int userId)
        {
            await _repository.MarkAllReadAsync(userId);
        }
    }
}
