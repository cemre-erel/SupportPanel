using System.ComponentModel.DataAnnotations;

namespace SupportPanel.Models
{
    public class SendNotificationViewModel
    {
        [Required(ErrorMessage = "Alıcı türü seçiniz.")]
        public string RecipientType { get; set; } = "Kullanıcı";

        [Range(1, int.MaxValue, ErrorMessage = "Kullanıcı seçiniz.")]
        public int? UserId { get; set; }

        public string? RoleName { get; set; }

        [Required(ErrorMessage = "Mesaj zorunludur.")]
        [StringLength(500, MinimumLength = 3, ErrorMessage = "Mesaj 3-500 karakter olmalıdır.")]
        public string Message { get; set; } = string.Empty;
    }
}
