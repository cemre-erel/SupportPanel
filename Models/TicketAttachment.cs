namespace SupportPanel.Models
{
    public class TicketAttachment
    {
        public int Id { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public DateTime UploadedDate { get; set; } = DateTime.Now;

        // Ticket FK
        public int TicketId { get; set; }

        public Ticket? Ticket { get; set; }

        // Dosyayı yükleyen kullanıcı
        public int? UploadedByUserId { get; set; }

        public User? UploadedByUser { get; set; }
    }
}