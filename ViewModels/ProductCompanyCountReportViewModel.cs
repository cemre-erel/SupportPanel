namespace SupportPanel.ViewModels
{
    public class ProductCompanyCountReportViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int OpenCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public int AssignedCount { get; set; }
        public int UnassignedCount { get; set; }
    }
}
