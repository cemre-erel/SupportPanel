using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupportPanel.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad zorunludur.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Ad 2-50 karakter olmalıdır.")]
        [RegularExpression(@"^[^\d]+$", ErrorMessage = "İsim sayı içeremez.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad zorunludur.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Soyad 2-50 karakter olmalıdır.")]
        [RegularExpression(@"^[^\d]+$", ErrorMessage = "İsim sayı içeremez.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-20 karakter olmalıdır.")]
        [RegularExpression(@"^\S+$", ErrorMessage = "Kullanıcı adı boşluk içeremez.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string PasswordHash { get; set; } = string.Empty;

        [NotMapped]
        [Compare("PasswordHash", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(100)]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int? TenantId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Bu alan zorunludur.")]
        public int RoleId { get; set; }

        public Tenant? Tenant { get; set; }

        public Role? Role { get; set; }

        public ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();

        public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();

        public ICollection<TicketComment> Comments { get; set; }
            = new List<TicketComment>();

        public ICollection<TicketAttachment> UploadedAttachments { get; set; }
            = new List<TicketAttachment>();

        public ICollection<TicketHistory> TicketHistory { get; set; }
            = new List<TicketHistory>();

        public ICollection<UserProduct> UserProducts { get; set; }
            = new List<UserProduct>();

        public ICollection<Notification> Notifications { get; set; }
            = new List<Notification>();
    }
}
