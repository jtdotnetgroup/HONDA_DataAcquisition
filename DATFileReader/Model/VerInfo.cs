using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATFileReader.Model
{
    /// <summary>
    /// 版本信息表
    /// </summary>
    public class VerInfo
    {   
        // FID
        public string FID { get; set; } 
        // 机号
        public string DeviceNum { get; set; }
        // 机种名
        public string MachineName { get; set; }
        // 操作者NO
        public string CZRNO { get; set; }
        // 二维码
        public string QR { get; set; }
        // 数据收集时间
        public string CollectionTime { get; set; }
        // 铸造加压时间(0.1s)
        public string StressTime { get; set; }
        // 制品(shot）NO
        public string ProductsNo { get; set; }
        // 数据保存  年月日
        public string YMD { get; set; }
        // 数据保存  时分秒
        public string HMS { get; set; }
        // 注汤前时间TTime
        public string TTime { get; set; }
        // 预备Prepare
        public string Prepare { get; set; }
        // 溶汤温度
        public string DissolvingTemperature { get; set; }
        // 铸造机加压时间
        public string HHStressTime { get; set; }
        // 铸造机冷却时间
        public string HHCoolingTime { get; set; }
        // 铸造机抽芯时间
        public string HHLooseCoreTime { get; set; }
        // 最终加压未使用
        public string EndStressNotUsed { get; set; }
        // S阀用下限值
        public string SValvesLower { get; set; }
        // S阀用上限值
        public string SValvesUpper { get; set; }
        // M阀用下限值
        public string MValvesLower { get; set; }
        // M阀用上限值
        public string MValvesUpper { get; set; }
        // 加压开始
        public string StressStart { get; set; }
        // 加压中
        public string Stressing { get; set; }
        // 类型
        public string DSType { get; set; }
        // 据点以及应用程序版本NO
        public string AppVerNo { get; set; }
    } 
}
