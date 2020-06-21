using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using IgorRig.Misc;
using log4net;

namespace IgorRig.Processes
{
    public class ClockDrift
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromHours(1);
        public static void Run()
        {
            if (!RigSingleton.Instance.GetHistorianConfig().HistEnvironment.ClockDriftCheck) return;
            var thread = new Thread(Check);
            thread.Start();
        }
        public static void Check()
        {
            try
            {
                do
                {
                    var networkTime = GetNetworkTime();
                    var universalTime = networkTime.ToUniversalTime();
                    var dateTimeNow = DateTime.Now;
                    var timeSpan = networkTime.Subtract(dateTimeNow);
                    if (timeSpan.TotalMinutes > 5 || timeSpan.TotalMinutes < -5)
                    {
                        Log.Warn($"System clock differs from ntpServer by {timeSpan.TotalMinutes} minutes.");
                        Log.Warn($"Setting time to {networkTime}. Currently set to {dateTimeNow}.");
                        SetSystemTime(universalTime);
                    }
                    Thread.Sleep(RepeatCheckEvery);
                } while (true);
            }
            catch (Exception e)
            {
                Log.Error("Failure in ClockDrift");
                Log.Error($"{e}");
            }
        }

        public struct SystemTime
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Millisecond;
        }

        [DllImport("kernel32.dll", EntryPoint = "GetSystemTime", SetLastError = true)]
        public extern static void Win32GetSystemTime(ref SystemTime sysTime);

        [DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
        public extern static bool Win32SetSystemTime(ref SystemTime sysTime);

        private static void SetSystemTime(DateTime dateTime)
        {
            //Copied from;
            //https://stackoverflow.com/questions/650849/change-system-date-programmatically
            // Set system date and time
            SystemTime updatedTime = new SystemTime
            {
                Year = (ushort) dateTime.Year,
                Month = (ushort) dateTime.Month,
                Day = (ushort) dateTime.Day,
                Hour = (ushort) dateTime.Hour,
                Minute = (ushort) dateTime.Minute,
                Second = (ushort) dateTime.Second
            };
            // Call the unmanaged function that sets the new date and time instantly
            Win32SetSystemTime(ref updatedTime);
        }
        public static DateTime GetNetworkTime()
        {
            // Copied from;
            // https://stackoverflow.com/questions/1193955/how-to-query-an-ntp-server-using-c
            //default Windows time server
            const string ntpServer = "time.windows.com";

            // NTP message size - 16 bytes of the digest (RFC 2030)
            var ntpData = new byte[48];

            //Setting the Leap Indicator, Version Number and Mode values
            ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;

            //The UDP port number assigned to NTP is 123
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            //NTP uses UDP

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);

                //Stops code hang if NTP is blocked
                socket.ReceiveTimeout = 3000;

                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();
            }

            //Offset to get to the "Transmit Timestamp" field (time at which the reply 
            //departed the server for the client, in 64-bit timestamp format."
            const byte serverReplyTime = 40;

            //Get the seconds part
            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            //Get the seconds fraction
            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            //Convert From big-endian to little-endian
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

            //**UTC** time
            var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);

            return networkDateTime.ToLocalTime();
        }

        // stackoverflow.com/a/3294698/162671
        static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                          ((x & 0x0000ff00) << 8) +
                          ((x & 0x00ff0000) >> 8) +
                          ((x & 0xff000000) >> 24));
        }
    }
}