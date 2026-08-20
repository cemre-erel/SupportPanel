namespace SupportPanel.ViewModels
{
    public class TicketSlaListItemViewModel
    {
        public string LevelName { get; set; } = string.Empty;
        public string State { get; set; } = "active";
        public string CssClass { get; set; } = "text-success";
        public string Summary { get; set; } = string.Empty;
        public string ResponseText { get; set; } = string.Empty;
        public string ResolutionText { get; set; } = string.Empty;
    }
}
