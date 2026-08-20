namespace SupportPanel.ViewModels
{
    public class ProductCountReportViewModel
    {
        public string ProductName { get; set; } = string.Empty;

        public int TotalCount { get; set; }

        public int OpenCount { get; set; }

        public int CompletedCount { get; set; }

        public int AssignedCount { get; set; }

        public int UnassignedCount { get; set; }

        public int SlaBreachedCount { get; set; }
    }
}
