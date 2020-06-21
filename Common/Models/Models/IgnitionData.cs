using System;
using System.Data;
using System.Reflection;
using log4net;

namespace Common.Models.Models
{
    public class IgnitionData
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public Int64 SqlTagId { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public string DataType { get; set; }
        public string OpcItemPath { get; set; }
        public string OpcServer { get; set; }
        public string Root { get; set; }
        public string HistoricalScanclass { get; set; }
        public string HistoryProvider { get; set; }
        public string TagPath => Path + '/' + Name;

        public IgnitionData()
        {
        }

        public IgnitionData(IDataRecord reader)
        {
            var field = "";
            object thing = null;
            try
            {
                field = "SqlTagId";
                thing = reader[0];
                if (reader[0] is decimal d)
                {
                    SqlTagId = Decimal.ToInt64(d);
                }
                else
                {
                    SqlTagId = (Int64)reader[0];
                }

                field = "Path";
                thing = reader[1];
                Path = (string)reader[1];

                field = "Name";
                thing = reader[2];
                Name = (string)reader[2];

                field = "DataType";
                thing = reader[3];
                DataType = (string)reader[3];

                field = "OpcItemPath";
                thing = reader[4];
                OpcItemPath = (string)reader[4];

                field = "OpcServer";
                thing = reader[5];
                OpcServer = (string)reader[5];

                field = "HistoricalScanClass";
                thing = reader[6];
                if (reader[6] is DBNull)
                {
                    HistoricalScanclass = "";
                }
                else
                {
                    HistoricalScanclass = (string)reader[6];
                }


                field = "HistoryProvider";
                thing = reader[7];
                HistoryProvider = (string)reader[7];
            }
            catch (Exception e)
            {
                Log.Warn($"Field: {field} Value: {thing}");
                Log.Error(e.Message);
            }
        }
    }
}