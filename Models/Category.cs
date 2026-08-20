using System.ComponentModel.DataAnnotations;

namespace SupportPanel.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Kategori adı 2-50 karakter olmalıdır.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Açıklama en fazla 200 karakter olabilir.")]
        public string Description { get; set; } = string.Empty;

        // Navigation Property
        public ICollection<Ticket> Tickets { get; set; }
            = new List<Ticket>();
    }
}
