using System.ComponentModel.DataAnnotations;

namespace SupportPanel.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [MaxLength(128)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime? UsedAtUtc { get; set; }

        public bool IsUsed => UsedAtUtc.HasValue;
        public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
        public bool IsValid => !IsUsed && !IsExpired;
    }
}