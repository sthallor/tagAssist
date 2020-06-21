namespace Common.Models.Reporting
{
    public class TagData
    {
        public string Rig { get; set; }
        public string Device { get; set; }
        public string ControlSystem { get; set; }
        public string Division { get; set; }
        public string TourSource { get; set; }
        private decimal? HoursSinceLastTour { get; set; }
        private decimal? HoursSinceLastTag { get; set; }
        private decimal? HoursSinceLastEdr { get; set; }
        public double GetHoursSinceLastTour() => decimal.ToDouble(HoursSinceLastTour.GetValueOrDefault());

        public double GetHoursSinceLastTag()
        {
            if (HoursSinceLastTag == null)
                return 480;
            return decimal.ToDouble(HoursSinceLastTag.GetValueOrDefault());
        }
        public double GetHoursSinceLastEdr() => decimal.ToDouble(HoursSinceLastEdr.GetValueOrDefault());
        public override string ToString()
        {
            return Device;
        }
    }
}