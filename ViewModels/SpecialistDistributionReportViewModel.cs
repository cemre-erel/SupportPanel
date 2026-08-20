namespace SupportPanel.ViewModels
{
    public class SpecialistDistributionReportViewModel
    {
        public List<SpecialistDistributionRowViewModel> Rows { get; set; } = new();
        public List<string> Products { get; set; } = new();
        public int TotalTickets { get; set; }
        public int AssignedTickets { get; set; }
        public int UnassignedTickets { get; set; }
        public int CompletedTickets { get; set; }
    }

    public class SpecialistDistributionRowViewModel
    {
        public int? SpecialistId { get; set; }
        public string SpecialistName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int OpenCount { get; set; }
        public int InReviewCount { get; set; }
        public int WaitingCustomerCount { get; set; }
        public int ResolvedCount { get; set; }
        public int ClosedCount { get; set; }
        public int SlaBreachedCount { get; set; }
        public double? AverageResponseMinutes { get; set; }
        public double? AverageResolutionMinutes { get; set; }
        public double? SlaSuccessRate { get; set; }
    }
}
