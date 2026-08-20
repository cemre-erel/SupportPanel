namespace SupportPanel.Services
{
    public static class SlaDurationFormatter
    {
        public static string FormatMinutes(int totalMinutes)
        {
            if (totalMinutes <= 0)
                return "0 dakika";

            var days = totalMinutes / 1440;
            var remainingAfterDays = totalMinutes % 1440;
            var hours = remainingAfterDays / 60;
            var minutes = remainingAfterDays % 60;
            var parts = new List<string>();

            if (days > 0)
                parts.Add($"{days} gün");

            if (hours > 0)
                parts.Add($"{hours} saat");

            if (minutes > 0)
                parts.Add($"{minutes} dakika");

            return string.Join(" ", parts);
        }

        public static string FormatDuration(TimeSpan duration)
        {
            var totalMinutes = (int)Math.Ceiling(Math.Abs(duration.TotalMinutes));
            return FormatMinutes(totalMinutes);
        }
    }
}
