using SupportPanel.Models;

namespace SupportPanel.Services
{
    public static class SlaCalculator
    {
        public static readonly TimeSpan WorkStart = new TimeSpan(9, 0, 0);
        public static readonly TimeSpan WorkEnd = new TimeSpan(18, 0, 0);

        public static bool IsWorkingTime(DateTime value)
        {
            if (value.DayOfWeek == DayOfWeek.Saturday || value.DayOfWeek == DayOfWeek.Sunday)
                return false;

            var timeOfDay = value.TimeOfDay;
            return timeOfDay >= WorkStart && timeOfDay < WorkEnd;
        }

        public static DateTime NextWorkingStart(DateTime value)
        {
            var current = value;

            while (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday)
            {
                current = current.Date.AddDays(1);
            }

            var start = current.Date.Add(WorkStart);

            if (start <= value)
            {
                start = start.AddDays(1);

                while (start.DayOfWeek == DayOfWeek.Saturday || start.DayOfWeek == DayOfWeek.Sunday)
                {
                    start = start.AddDays(1);
                }
            }

            return start;
        }

        public static TimeSpan CalculateElapsed(DateTime start, DateTime end, SlaCalculationMethod method,
            IEnumerable<(DateTime StartDate, DateTime? EndDate)>? pauses = null)
        {
            if (end <= start)
                return TimeSpan.Zero;

            var total = TimeSpan.Zero;
            var cursor = start;

            var pauseRanges = (pauses ?? Array.Empty<(DateTime, DateTime?)>())
                .Select(p => (
                    Start: p.StartDate < start ? start : p.StartDate,
                    End: (p.EndDate ?? end) > end ? end : (p.EndDate ?? end)))
                .Where(p => p.End > p.Start)
                .OrderBy(p => p.Item1)
                .ToList();

            var mergedPauses = new List<(DateTime Start, DateTime End)>();

            foreach (var pause in pauseRanges)
            {
                if (mergedPauses.Count == 0 || pause.Start > mergedPauses[^1].End)
                {
                    mergedPauses.Add(pause);
                }
                else if (pause.End > mergedPauses[^1].End)
                {
                    var previous = mergedPauses[^1];
                    mergedPauses[^1] = (previous.Start, pause.End);
                }
            }

            foreach (var pause in mergedPauses)
            {
                if (pause.Start > cursor)
                {
                    total += CalculateSegment(cursor, pause.Start, method);
                }

                if (pause.End > cursor)
                    cursor = pause.End;
            }

            if (cursor < end)
                total += CalculateSegment(cursor, end, method);

            return total;
        }

        private static TimeSpan CalculateSegment(DateTime start, DateTime end, SlaCalculationMethod method)
        {
            return method == SlaCalculationMethod.WorkingHours
                ? CalculateWorkingTime(start, end)
                : end - start;
        }

        public static TimeSpan CalculateWorkingTime(DateTime start, DateTime end)
        {
            var total = TimeSpan.Zero;
            var cursor = start;

            while (cursor < end)
            {
                if (!IsWorkingTime(cursor))
                {
                    cursor = IsWorkingTime(cursor) ? cursor : NextWorkingStart(cursor);
                    continue;
                }

                var workEnd = cursor.Date.Add(WorkEnd);
                var chunkEnd = workEnd < end ? workEnd : end;

                total += chunkEnd - cursor;
                cursor = chunkEnd;
            }

            return total;
        }

        public static bool IsResponseBreached(DateTime createdDate, DateTime? firstResponseDate, SlaLevel slaLevel,
            SlaCalculationMethod calculationMethod,
            IEnumerable<(DateTime StartDate, DateTime? EndDate)>? pauses = null,
            DateTime? evaluationDate = null)
        {
            var elapsed = CalculateElapsed(createdDate, firstResponseDate ?? evaluationDate ?? DateTime.Now,
                calculationMethod, pauses);

            return elapsed.TotalMinutes > slaLevel.ResponseTargetMinutes;
        }

        public static bool IsResolutionBreached(DateTime createdDate, DateTime? resolvedDate, SlaLevel slaLevel,
            SlaCalculationMethod calculationMethod,
            IEnumerable<(DateTime StartDate, DateTime? EndDate)>? pauses = null,
            DateTime? evaluationDate = null)
        {
            var elapsed = CalculateElapsed(createdDate, resolvedDate ?? evaluationDate ?? DateTime.Now,
                calculationMethod, pauses);

            return elapsed.TotalMinutes > slaLevel.ResolutionTargetMinutes;
        }

        public static SlaTrackingResult CalculateTracking(
            Ticket ticket,
            SlaLevel slaLevel,
            SlaCalculationMethod calculationMethod,
            IEnumerable<(DateTime StartDate, DateTime? EndDate)>? pauses = null,
            DateTime? evaluationDate = null)
        {
            var now = evaluationDate ?? DateTime.Now;
            if (!ticket.SlaStartedDate.HasValue)
            {
                return new SlaTrackingResult
                {
                    CalculationMethod = calculationMethod,
                    IsStarted = false,
                    ResponseElapsed = TimeSpan.Zero,
                    ResponseRemaining = TimeSpan.FromMinutes(slaLevel.ResponseTargetMinutes),
                    ResolutionElapsed = TimeSpan.Zero,
                    ResolutionRemaining = TimeSpan.FromMinutes(slaLevel.ResolutionTargetMinutes)
                };
            }

            var slaStart = ticket.SlaStartedDate.Value;
            var isCancelled = ticket.Status == TicketStatus.Cancelled;
            var responseCompleted = ticket.FirstResponseDate.HasValue;
            var resolutionCompleted = ticket.ResolvedDate.HasValue || ticket.ClosedDate.HasValue;
            var terminalDate = ticket.CancelledDate ?? ticket.ClosedDate ?? ticket.ResolvedDate;
            var responseEnd = ticket.FirstResponseDate ?? terminalDate ?? now;
            var resolutionEnd = ticket.ResolvedDate ?? ticket.ClosedDate ?? ticket.CancelledDate ?? now;

            var responseElapsed = CalculateElapsed(slaStart, responseEnd, calculationMethod, pauses);
            var resolutionElapsed = CalculateElapsed(slaStart, resolutionEnd, calculationMethod, pauses);
            var responseRemaining = TimeSpan.FromMinutes(slaLevel.ResponseTargetMinutes) - responseElapsed;
            var resolutionRemaining = TimeSpan.FromMinutes(slaLevel.ResolutionTargetMinutes) - resolutionElapsed;

            return new SlaTrackingResult
            {
                CalculationMethod = calculationMethod,
                IsStarted = true,
                ResponseElapsed = responseElapsed,
                ResponseRemaining = responseRemaining,
                IsResponseCompleted = responseCompleted,
                IsResponseStopped = isCancelled && !responseCompleted,
                IsResponseBreached = !isCancelled && responseRemaining < TimeSpan.Zero,
                ResolutionElapsed = resolutionElapsed,
                ResolutionRemaining = resolutionRemaining,
                IsResolutionCompleted = resolutionCompleted,
                IsResolutionStopped = isCancelled && !resolutionCompleted,
                IsResolutionBreached = !isCancelled && resolutionRemaining < TimeSpan.Zero
            };
        }
    }
}
