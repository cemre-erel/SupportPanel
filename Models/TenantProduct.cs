using System.ComponentModel.DataAnnotations;

namespace SupportPanel.Models
{
    public class TenantProduct
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Firma seçiniz.")]
        public int TenantId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Ürün seçiniz.")]
        public int ProductId { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public Tenant? Tenant { get; set; }

        public Product? Product { get; set; }
    }
}