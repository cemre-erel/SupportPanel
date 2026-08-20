using System.ComponentModel.DataAnnotations;

namespace SupportPanel.Models
{
    public class EmailSetting
    {
        public int Id { get; set; }
        public bool Enabled { get; set; }

        [MaxLength(255)]
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;

        [MaxLength(255)]
        public string? Username { get; set; }

        public string? ProtectedPassword { get; set; }

        [MaxLength(255)]
        public string FromAddress { get; set; } = string.Empty;

        [MaxLength(150)]
        public string FromName { get; set; } = "SupportPanel";

        public DateTime UpdatedDate { get; set; } = DateTime.Now;
        public int? UpdatedByUserId { get; set; }
    }
}
