using System;

namespace IgorRig.Misc
{
    public class Utility
    {
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            try
            {
                var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddSeconds(unixTimeStamp / 1000);
                return dtDateTime.ToLocalTime();
            }
            catch (Exception)
            {
                var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddSeconds(unixTimeStamp / 1000000);
                return dtDateTime.ToLocalTime();
            }
        }
    }
}