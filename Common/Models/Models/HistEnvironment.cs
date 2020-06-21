namespace Common.Models.Models
{
    public class HistEnvironment
    {
        public string Host { get; set; }
        public string Ip { get; set; }
        public string Os { get; set; }
        public string Db { get; set; }
        public string Java { get; set; }
        public string License { get; set; }
        public bool RealTimeRigState { get; set; }
        public bool RealTimeIGBT { get; set; }
        public bool RealTimeMSE { get; set; }
        public bool ClockDriftCheck { get; set; }
        public bool ResetTrial { get; set; }
    }
}