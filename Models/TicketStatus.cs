namespace SupportPanel.Models
{
    public static class TicketStatus
    {
        public const string New = "New";

        public const string SupportQueue = "SupportQueue";

        public const string Assigned = "Assigned";

        public const string InReview = "InReview";

        public const string WaitingCustomer = "WaitingCustomer";

        public const string Resolved = "Resolved";

        public const string Closed = "Closed";

        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { New, SupportQueue, Assigned, InReview, WaitingCustomer, Resolved, Closed, Cancelled };

        public static readonly IReadOnlyDictionary<string, string> DisplayNames =
            new Dictionary<string, string>
            {
                { New, "Yeni" },
                { SupportQueue, "Destek Havuzunda" },
                { Assigned, "Atandı" },
                { InReview, "İnceleniyor" },
                { WaitingCustomer, "Müşteri Bekleniyor" },
                { Resolved, "Çözüldü" },
                { Closed, "Kapatıldı" },
                { Cancelled, "İptal Edildi" }
            };

        public static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions =
            new Dictionary<string, string[]>
            {
                { New, new[] { SupportQueue, Assigned, Closed } },
                { SupportQueue, new[] { Assigned } },
                { Assigned, new[] { InReview, WaitingCustomer, Resolved } },
                { InReview, new[] { Resolved, WaitingCustomer } },
                { WaitingCustomer, new[] { InReview, Resolved } },
                { Resolved, new[] { InReview, Closed } },
                { Closed, new string[0] },
                { Cancelled, new string[0] }
            };

        public static bool CanCancel(string status)
        {
            return status != Closed && status != Cancelled;
        }

        public static string GetReopenTarget(int? assignedUserId, DateTime? slaStartedDate)
        {
            if (assignedUserId.HasValue)
                return InReview;

            if (slaStartedDate.HasValue)
                return SupportQueue;

            return New;
        }
    }
}
