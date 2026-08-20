using System.ComponentModel.DataAnnotations;

namespace SupportPanel.Models.ViewModels
{
    public class EmailSettingsViewModel
    {
        public bool Enabled { get; set; }

        [Display(Name = "SMTP sunucusu")]
        public string Host { get; set; } = string.Empty;

        [Range(1, 65535, ErrorMessage = "Port 1-65535 arasında olmalıdır.")]
        public int Port { get; set; } = 587;

        [Display(Name = "Güvenli bağlantı (SSL/TLS)")]
        public bool EnableSsl { get; set; } = true;

        [Display(Name = "Kullanıcı adı")]
        public string? Username { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Şifre / uygulama parolası")]
        public string? Password { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir gönderici e-posta adresi giriniz.")]
        [Display(Name = "Gönderici e-posta adresi")]
        public string FromAddress { get; set; } = string.Empty;

        [Display(Name = "Gönderici adı")]
        public string FromName { get; set; } = "SupportPanel";

        [EmailAddress(ErrorMessage = "Geçerli bir test e-posta adresi giriniz.")]
        [Display(Name = "Test alıcısı")]
        public string? TestRecipient { get; set; }

        public bool HasSavedPassword { get; set; }
    }
}
