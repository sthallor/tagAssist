namespace Common.Models.Ignition
{
    public class Trial
    {
        public string trialState { get; set; }
        public int remainingSeconds { get; set; }
        public string remainingDisplay { get; set; }
        public bool expired { get; set; }
        public bool emergency { get; set; }
        public string emergencyTimeLeft { get; set; }
    }
}