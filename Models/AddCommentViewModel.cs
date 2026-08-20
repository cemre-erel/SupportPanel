namespace SupportPanel.Models
{
    public class AddCommentViewModel
    {
        public int TicketId { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Mesaj boş olamaz.")]
        [System.ComponentModel.DataAnnotations.StringLength(2000, MinimumLength = 2, ErrorMessage = "Mesaj en az 2 karakter olmalıdır.")]
        public string Message { get; set; } = string.Empty;
    }
}
