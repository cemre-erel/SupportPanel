using Microsoft.AspNetCore.Http;

namespace SupportPanel.Models
{
    public class UploadAttachmentViewModel
    {
        public int TicketId { get; set; }

        public IFormFile? File { get; set; }
    }
}