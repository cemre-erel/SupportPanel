using SupportPanel.Models;
using SupportPanel.Services;
using Xunit;

namespace SupportPanel.Tests
{
    public class SlaCalculatorTests
    {
        [Theory]
        [InlineData("2026-08-17 10:00", true)]   // Pazartesi - mesai içi
        [InlineData("2026-08-17 19:00", false)]  // Pazartesi - mesai dışı
        [InlineData("2026-08-15 10:00", false)]  // Cumartesi
        [InlineData("2026-08-16 10:00", false)]  // Pazar
        [InlineData("2026-08-17 09:00", true)]   // Mesai başlangıcı
        [InlineData("2026-08-17 18:00", false)]  // Mesai bitişi
        public void IsWorkingTime_DogruSonucDoner(string dateStr, bool expected)
        {
            var date = DateTime.Parse(dateStr);

            var result = SlaCalculator.IsWorkingTime(date);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void CalculateWorkingTime_MesaiDisindakiSureleriSaymamalidir()
        {
            var start = new DateTime(2026, 8, 17, 17, 0, 0); // Pazartesi
            var end = new DateTime(2026, 8, 18, 10, 0, 0);   // Salı

            var result = SlaCalculator.CalculateWorkingTime(start, end);

            Assert.Equal(TimeSpan.FromHours(2), result);
        }

        [Fact]
        public void CalculateWorkingTime_HaftaSonunuSaymamalidir()
        {
            var start = new DateTime(2026, 8, 14, 17, 0, 0); // Cuma
            var end = new DateTime(2026, 8, 17, 10, 0, 0);   // Pazartesi

            var result = SlaCalculator.CalculateWorkingTime(start, end);

            Assert.Equal(TimeSpan.FromHours(2), result);
        }

        [Fact]
        public void CalculateElapsed_PauseSuresiniToplamdanCikarmalidir()
        {
            var start = new DateTime(2026, 8, 17, 10, 0, 0);
            var end = new DateTime(2026, 8, 17, 14, 0, 0);

            var pauses = new List<(DateTime StartDate, DateTime? EndDate)>
            {
                (
                    new DateTime(2026, 8, 17, 11, 0, 0),
                    new DateTime(2026, 8, 17, 12, 0, 0)
                )
            };

            var result = SlaCalculator.CalculateElapsed(
                start,
                end,
                SlaCalculationMethod.AroundTheClock,
                pauses);

            Assert.Equal(TimeSpan.FromHours(3), result);
        }

        [Fact]
        public void CalculateTracking_SlaBaslamadiysaIsStartedFalseDonmelidir()
        {
            var ticket = new Ticket
            {
                SlaStartedDate = null
            };

            var slaLevel = new SlaLevel
            {
                ResponseTargetMinutes = 30,
                ResolutionTargetMinutes = 120
            };

            var result = SlaCalculator.CalculateTracking(
                ticket,
                slaLevel,
                SlaCalculationMethod.AroundTheClock);

            Assert.False(result.IsStarted);
            Assert.Equal(TimeSpan.Zero, result.ResponseElapsed);
            Assert.Equal(TimeSpan.Zero, result.ResolutionElapsed);
        }

        [Fact]
        public void CalculateTracking_IptalEdilenTalepteIhlalOlmamali()
        {
            var start = new DateTime(2026, 8, 10, 9, 0, 0);
            var evaluationDate = new DateTime(2026, 8, 17, 12, 0, 0);

            var ticket = new Ticket
            {
                SlaStartedDate = start,
                Status = TicketStatus.Cancelled,
                CancelledDate = evaluationDate
            };

            var slaLevel = new SlaLevel
            {
                ResponseTargetMinutes = 30,
                ResolutionTargetMinutes = 120
            };

            var result = SlaCalculator.CalculateTracking(
                ticket,
                slaLevel,
                SlaCalculationMethod.AroundTheClock,
                evaluationDate: evaluationDate);

            Assert.False(result.IsResponseBreached);
            Assert.False(result.IsResolutionBreached);
        }

        [Fact]
        public void CalculateTracking_IlkMudahaleSuresiAsildiysaResponseBreachedTrueOlmali()
        {
            var start = new DateTime(2026, 8, 17, 10, 0, 0);
            var evaluationDate = new DateTime(2026, 8, 17, 11, 0, 0);

            var ticket = new Ticket
            {
                SlaStartedDate = start
            };

            var slaLevel = new SlaLevel
            {
                ResponseTargetMinutes = 30,
                ResolutionTargetMinutes = 120
            };

            var result = SlaCalculator.CalculateTracking(
                ticket,
                slaLevel,
                SlaCalculationMethod.AroundTheClock,
                evaluationDate: evaluationDate);

            Assert.True(result.IsResponseBreached);
        }

        [Fact]
        public void CalculateTracking_CozumSuresiAsildiysaResolutionBreachedTrueOlmali()
        {
            var start = new DateTime(2026, 8, 17, 10, 0, 0);
            var evaluationDate = new DateTime(2026, 8, 17, 13, 0, 0);

            var ticket = new Ticket
            {
                SlaStartedDate = start
            };

            var slaLevel = new SlaLevel
            {
                ResponseTargetMinutes = 30,
                ResolutionTargetMinutes = 120
            };

            var result = SlaCalculator.CalculateTracking(
                ticket,
                slaLevel,
                SlaCalculationMethod.AroundTheClock,
                evaluationDate: evaluationDate);

            Assert.True(result.IsResolutionBreached);
        }
    }
}