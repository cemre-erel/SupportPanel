using System.ComponentModel.DataAnnotations;

namespace SupportPanel.Models
{
    public class TicketComment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Yorum boş olamaz.")]
        [StringLength(2000, MinimumLength = 2, ErrorMessage = "Yorum en az 2 karakter olmalıdır.")]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Ticket FK
        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        // Yorumu yazan kullanıcı
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}