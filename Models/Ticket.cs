using System.ComponentModel.DataAnnotations;

namespace SupportPanel.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Başlık zorunludur.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Başlık 3-150 karakter olmalıdır.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama zorunludur.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Açıklama en az 10 karakter olmalıdır.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Öncelik seçiniz.")]
        [RegularExpression("^(Low|Medium|High|Critical)$", ErrorMessage = "Geçersiz öncelik seçimi.")]
        public string Priority { get; set; } = string.Empty;

        public string Status { get; set; } = "New";

        [Range(1, int.MaxValue, ErrorMessage = "SLA seviyesi seçiniz.")]
        public int SlaLevelId { get; set; }

        // Tarihler
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? SlaStartedDate { get; set; }

        public DateTime? FirstResponseDate { get; set; }

        public DateTime? ResolvedDate { get; set; }

        public DateTime? ClosedDate { get; set; }

        public DateTime? CancelledDate { get; set; }

        // Foreign Keys
        [Range(1, int.MaxValue, ErrorMessage = "Firma seçiniz.")]
        public int TenantId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Ürün seçiniz.")]
        public int ProductId { get; set; }

        public int? CreatedByUserId { get; set; }

        public int? AssignedUserId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Kategori seçiniz.")]
        public int CategoryId { get; set; }

        // Navigation Properties
        public Tenant? Tenant { get; set; }

        public Product? Product { get; set; }

        public User? CreatedByUser { get; set; }

        public User? AssignedUser { get; set; }

        public SlaLevel? SlaLevel { get; set; }

        public ICollection<TicketComment> Comments { get; set; }
            = new List<TicketComment>();

        public ICollection<TicketAttachment> Attachments { get; set; }
            = new List<TicketAttachment>();

        public ICollection<TicketHistory> History { get; set; }
            = new List<TicketHistory>();

        public ICollection<SlaPause> SlaPauses { get; set; }
            = new List<SlaPause>();

        public Category? Category { get; set; }
    }
}
