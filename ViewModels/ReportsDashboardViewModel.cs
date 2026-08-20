using SupportPanel.Models;

namespace SupportPanel.ViewModels
{
    public class ReportsDashboardViewModel
    {
        public int TotalTickets { get; set; }

        public int OpenTickets { get; set; }

        public int ResolvedTickets { get; set; }

        public int ClosedTickets { get; set; }

        public int CancelledTickets { get; set; }

        public int SlaBreachedTickets { get; set; }
        public int ActiveSlaTickets { get; set; }
        public int WarningSlaTickets { get; set; }
        public int ResponseBreachedTickets { get; set; }
        public int ResolutionBreachedTickets { get; set; }
        public double AverageResponseMinutes { get; set; }
        public double AverageResolutionMinutes { get; set; }
        public double ResponseComplianceRate { get; set; }
        public double ResolutionComplianceRate { get; set; }
        public List<ReportTicketRowViewModel> TicketRows { get; set; } = new();
        public List<string> Products { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public List<string> SlaLevels { get; set; } = new();
        public List<string> Tenants { get; set; } = new();
        public List<ReportAssigneeOption> Assignees { get; set; } = new();

        public List<Ticket> RecentTickets { get; set; } = new List<Ticket>();
    }

    public class ReportAssigneeOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ReportTicketRowViewModel
    {
        public int TicketId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string SlaLevelName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SlaState { get; set; } = string.Empty;
        public string SlaStateText { get; set; } = string.Empty;
        public string ViolationType { get; set; } = "-";
        public string DelayText { get; set; } = "-";
        public string AssignedUserName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public bool IsResponseCompleted { get; set; }
        public bool IsResolutionCompleted { get; set; }
        public bool IsResponseBreached { get; set; }
        public bool IsResolutionBreached { get; set; }
        public double ResponseElapsedMinutes { get; set; }
        public double ResolutionElapsedMinutes { get; set; }
    }
}
