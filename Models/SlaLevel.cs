using System.ComponentModel.DataAnnotations;

namespace SupportPanel.Models
{
    public class SlaLevel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "SLA seviyesi adı zorunludur.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "SLA seviyesi adı 2-50 karakter olmalıdır.")]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessage = "Firma seçiniz.")]
        public int TenantId { get; set; }

        [Range(1, 525600, ErrorMessage = "İlk müdahale hedefi 1-525600 dakika olmalıdır.")]
        public int ResponseTargetMinutes { get; set; } = 480;

        [Range(1, 525600, ErrorMessage = "Çözüm hedefi 1-525600 dakika olmalıdır.")]
        public int ResolutionTargetMinutes { get; set; } = 2880;

        public Tenant? Tenant { get; set; }
    }
}
