using System.ComponentModel.DataAnnotations;

namespace SupportPanel.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Kullanıcı seçiniz.")]
        public int? UserId { get; set; }

        public int? TicketId { get; set; }

        [Required(ErrorMessage = "Mesaj zorunludur.")]
        [StringLength(500, ErrorMessage = "Mesaj en fazla 500 karakter olmalıdır.")]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public User? User { get; set; }

        public Ticket? Ticket { get; set; }
    }
}
