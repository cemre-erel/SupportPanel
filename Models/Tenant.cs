using System.ComponentModel.DataAnnotations;

namespace SupportPanel.Models
{
    public class Tenant
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Firma adı zorunludur.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Firma adı 2-150 karakter olmalıdır.")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "İletişim kişisi zorunludur.")]
        [StringLength(100)]
        [RegularExpression(@"^[^\d]+$", ErrorMessage = "İsim sayı içeremez.")]
        public string ContactName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon zorunludur.")]
        [RegularExpression(@"^(\+90|0)?[5-9][0-9]{9}$", ErrorMessage = "Geçerli bir telefon numarası giriniz (örn. 05XX XXX XX XX).")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "SLA Hesaplama Yöntemi")]
        public SlaCalculationMethod SLACalculationMethod { get; set; }
            = SlaCalculationMethod.WorkingHours;

        public bool IsActive { get; set; } = true;

        public ICollection<User> Users { get; set; } = new List<User>();

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

        public ICollection<TenantProduct> TenantProducts { get; set; }
            = new List<TenantProduct>();

        public ICollection<SlaLevel> SlaLevels { get; set; }
            = new List<SlaLevel>();
    }
}
