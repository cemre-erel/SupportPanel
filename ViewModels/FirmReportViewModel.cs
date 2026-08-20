namespace SupportPanel.ViewModels
{
    public class FirmReportViewModel
    {
        public string CompanyName { get; set; } = string.Empty;

        public int TotalCount { get; set; }

        public int OpenCount { get; set; }

        public int ResolvedCount { get; set; }
    }
}
