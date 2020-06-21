using System;

namespace Common.Models.Reporting
{
    public class RigRemarks
    {
        public string RemarkType { get; set; }
        public string Remark { get; set; }
        public DateTimeOffset EffectiveDate { get; set; }
        public override string ToString()
        {
            return $"RemarkType: {RemarkType}  Remark:{Remark}  Date:{EffectiveDate:s}";
        }
    }
}