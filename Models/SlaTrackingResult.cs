namespace SupportPanel.Models
{
    public class SlaTrackingResult
    {
        public SlaCalculationMethod CalculationMethod { get; set; }

        public bool IsStarted { get; set; }

        public TimeSpan ResponseElapsed { get; set; }
        public TimeSpan ResponseRemaining { get; set; }
        public bool IsResponseCompleted { get; set; }
        public bool IsResponseStopped { get; set; }
        public bool IsResponseBreached { get; set; }

        public TimeSpan ResolutionElapsed { get; set; }
        public TimeSpan ResolutionRemaining { get; set; }
        public bool IsResolutionCompleted { get; set; }
        public bool IsResolutionStopped { get; set; }
        public bool IsResolutionBreached { get; set; }
    }
}
