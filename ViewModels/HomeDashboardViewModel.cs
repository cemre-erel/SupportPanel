using SupportPanel.Models;

namespace SupportPanel.ViewModels
{
    public class HomeDashboardViewModel
    {
        public int TotalTickets { get; set; }

        public int NewTickets { get; set; }

        public int AssignedTickets { get; set; }

        public int InReviewTickets { get; set; }

        public int WaitingCustomerTickets { get; set; }

        public int ResolvedTickets { get; set; }

        public int ClosedTickets { get; set; }

        public int CancelledTickets { get; set; }

        public int CriticalTickets { get; set; }

        public int TotalTenants { get; set; }

        public int TotalUsers { get; set; }

        public int TotalProducts { get; set; }

        public List<Ticket> RecentTickets { get; set; } = new List<Ticket>();
    }
}
