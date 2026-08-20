using System.ComponentModel.DataAnnotations;

namespace SupportPanel.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ürün adı zorunludur.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Ürün adı 2-100 karakter olmalıdır.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(30, ErrorMessage = "Versiyon en fazla 30 karakter olabilir.")]
        public string Version { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties

        public ICollection<Ticket> Tickets { get; set; }
            = new List<Ticket>();

        public ICollection<TenantProduct> TenantProducts { get; set; }
            = new List<TenantProduct>();

        public ICollection<UserProduct> UserProducts { get; set; }
            = new List<UserProduct>();
    }
}