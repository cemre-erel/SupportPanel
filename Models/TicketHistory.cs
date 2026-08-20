namespace SupportPanel.Models
{
    public class TicketHistory
    {
        public int Id { get; set; }

        public string Action { get; set; } = string.Empty;

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public DateTime ActionDate { get; set; } = DateTime.Now;

        // Ticket
        public int TicketId { get; set; }

        public Ticket? Ticket { get; set; }

        // İşlemi yapan kullanıcı
        public int? UserId { get; set; }

        public User? User { get; set; }
    }
}