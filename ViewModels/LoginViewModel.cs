using System.ComponentModel.DataAnnotations;

namespace SupportPanel.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı veya e-posta zorunludur.")]
        [StringLength(100, ErrorMessage = "Kullanıcı adı veya e-posta en fazla 100 karakter olabilir.")]
        [Display(Name = "Kullanıcı adı veya e-posta")]
        public string LoginIdentifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(100, ErrorMessage = "Şifre en fazla 100 karakter olabilir.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
