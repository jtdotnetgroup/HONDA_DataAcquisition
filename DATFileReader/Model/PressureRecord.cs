using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATFileReader.Model
{
    /// <summary>
    /// 加压记录
    /// </summary>
    public class PressureRecord
    {
        public string FID { get; set; }
        public string VerInfoID { get; set; }
        public string DicCode { get; set; }
        public string DicCode2 { get; set; }
        public string DicCode3 { get; set; }
        public string RecordTime { get; set; }
        public string RecordVal { get; set; } 
    }
}
