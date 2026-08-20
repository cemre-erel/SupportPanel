using System.ComponentModel.DataAnnotations;

namespace SupportPanel.Models
{
    public class SlaPause
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Talep seçiniz.")]
        public int TicketId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public Ticket? Ticket { get; set; }
    }
}
