using DATFileReader.Helper;
using DATFileReader.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows.Forms;
using System.Data.Linq;
using System.Linq;
using System.Threading.Tasks;
using hn.Common;

namespace DATFileReader
{
    public partial class FrmMain : Form
    {
        private bool isRunning { get; set; }
        #region 窗体
        public FrmMain()
        {
            InitializeComponent();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            Init();
            var scanInterval =Convert.ToInt32( ConfigurationManager.AppSettings["ScanInterval"])/1000;
            numInterval.Value = scanInterval;
            LogHelper.Init(new TextBoxWriter(txtLog));
        }
        #endregion

        #region 定义初始化字典数据
        Dictionary<string, string> dicCode = new Dictionary<string, string>()
        {

        };
        string vSQL = "";
        string DeviceNum = ConfigurationManager.AppSettings["DeviceNum"].ToString();
        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        void Init()
        {
            if (!MySqlHelper.IsOpen())
            {
                MessageBox.Show("连接失败！");
            }
        }
        /// <summary>
        /// 浏览文件
        /// </summary> 
        private void btnPath_Click(object sender, EventArgs e)
        {
            using (var ofd = new FolderBrowserDialog())
            {
                ofd.Description = "请选择DAT文件所在目录";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = ofd.SelectedPath;
                }
            }

          
        }
        /// <summary>
        /// 开始采集
        /// </summary>
        private void btnStart_Click(object sender, EventArgs e)
        {

            Timer timer=new Timer();
            timer.Interval = Convert.ToInt32(numInterval.Value*100);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var dirPath = txtPath.Text;
            if (!isRunning)
            {
                var task = new Task(() =>
                {
                    isRunning = true;
                    try
                    {
                        var filesName = DatFileReader.ScanDir(dirPath);
                        
                        foreach (var f in filesName)
                        {
                            LogHelper.Info($"解释文件【{f}】");
                            var data = DatFileReader.Open(f);
                            string[] row = data.Split(new string[] { ",\r\n" }, StringSplitOptions.None);
                            string StrType = f.Substring(f.Length - 7).Substring(0, 3).ToUpper();
                            switch (StrType)
                            {
                                case "STA":
                                {
                                    STAAnalysis(row);
                                    break;
                                }
                                case "DAT":
                                {
                                    DATAnalysis(row);
                                    break;
                                }
                            }
                        }

                        isRunning = false;

                    }
                    catch (Exception exception)
                    {
                        LogHelper.Error(exception);
                        isRunning = false;
                    }
                });

                task.Start();
            }
        }

        /// <summary>
        /// DAT文件解析
        /// </summary>
        /// <param name="row"></param>
        void DATAnalysis(string[] row)
        {
            #region 读取DAT信息
            string verInfo = row[0];    // 版本信息
            string[] tmpInfo = verInfo.Split(new string[] { "," }, StringSplitOptions.None);
            int rowNum = Convert.ToInt32(tmpInfo[tmpInfo.Length - 1]) + 1;
            bool IsOne = true;          // 去除第一条
            bool IsNewTable = false;    // 是否需要插入到新表
            int intAdd = 0;             // 判断出现两次可以转回为Int的就插入新的表
            int vsNum = 0;              // 判断int作用
            int i = 1;                  // 
            // 第一个表的数据
            Dictionary<int, Dictionary<string, string>> dic = new Dictionary<int, Dictionary<string, string>>();
            Dictionary<string, string> dicRow = new Dictionary<string, string>();
            //string[] code = { "", "実態金型温度", "FB控制用模具温度", "FB控制用目标温度", "冷却阀ON.・OFF状态", "吸管温度" };
            string[] code = { "", "HotMetalTemperature", "MoldTemperatureForFBControl", "TargetTemperatureForFBControl", "CoolingValveOn.Off", "PipetteTemperature" };
            // 其余表的数据
            Dictionary<string, Dictionary<string, string>> TowdicRow = new Dictionary<string, Dictionary<string, string>>();
            //string[] DataType = { "", "加圧用実態圧力", "加圧用目標圧力", "加圧用ﾚｷﾞｭﾚｰﾀｰ出力値" };
            string[] DataType = { "", "PressureOfPressurizedState", "TargetPressureForPressurization", "LevelOutputValueForPressurization" };
            int intDataType = 1;
            // 开始循环解析数据
            foreach (var tmp in row)
            {
                // 去除第一条
                if (IsOne) { IsOne = false; continue; }
                // 判断是否需要插入到新的表，判断出现两次可以转回为Int的就插入新的表
                intAdd = Int32.TryParse(tmp, out vsNum) ? intAdd + 1 : 0;
                IsNewTable = (intAdd == 2) || IsNewTable;
                // 是否新表
                if (!IsNewTable)
                {
                    if (i == 1)
                    {
                        dicRow = new Dictionary<string, string>();
                    }
                    dicRow.Add(code[i], tmp);
                    if (i == 5)
                    {
                        dic.Add(dic.Count + 1, dicRow);
                        i = 0;
                    }
                    i++;
                }
                // 下面每个表的
                else
                {
                    if (i == 1)
                    {
                        dicRow = new Dictionary<string, string>();
                    }
                    dicRow.Add((i * 0.1).ToString("0.0"), tmp);
                    i++;
                    if (i % rowNum == 0)
                    {
                        TowdicRow.Add(DataType[intDataType], dicRow);
                        i = 1;
                        intDataType++;
                    }
                }
            }
            #endregion

            #region 开始形成Class
            VerInfo vi = new VerInfo()
            {
                FID = Guid.NewGuid().ToString(),
                DSType = "DAT",
                AppVerNo = tmpInfo[0],
                QR = tmpInfo[1],
                CollectionTime = tmpInfo[2],
                StressTime = tmpInfo[3],
                DeviceNum = DeviceNum,
            };
            List<TEMPerA> temperaList = new List<TEMPerA>();
            TEMPerA tempera = null;
            List<string> tmpStringList = new List<string>();
            string[] tmpList = null;
            string[] tmpList2 = { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
            string[] tmpList3 = { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
            foreach (var item in dic)
            {
                foreach (var tmp in item.Value)
                {
                    tmpList = tmp.Value.Split(new string[] { "," }, StringSplitOptions.None);
                    if (tmpList.Length == 12)
                    {
                        tmpStringList = new List<string>();
                        tmpStringList.AddRange(tmpList);
                        tmpStringList.AddRange(tmpList2);
                        tmpList = null;
                        tmpList = tmpStringList.ToArray();
                    }
                    if (tmpList.Length == 1)
                    {
                        tmpStringList = new List<string>();
                        tmpStringList.AddRange(tmpList);
                        tmpStringList.AddRange(tmpList3);
                        tmpList = null;
                        tmpList = tmpStringList.ToArray();
                    }
                    //
                    tempera = new TEMPerA()
                    {
                        FID = Guid.NewGuid().ToString(),
                        VerInfoID = vi.FID,
                        DicCode = tmp.Key,
                        CH1M = tmpList[0],
                        CH1S = tmpList[1],
                        CH2M = tmpList[2],
                        CH2S = tmpList[3],
                        CH3M = tmpList[4],
                        CH3S = tmpList[5],
                        CH4M = tmpList[6],
                        CH4S = tmpList[7],
                        CH5M = tmpList[8],
                        CH5S = tmpList[9],
                        CH6M = tmpList[10],
                        CH6S = tmpList[11],
                        CH7M = tmpList[12],
                        CH7S = tmpList[13],
                        CH8M = tmpList[14],
                        CH8S = tmpList[15],
                        CH9M = tmpList[16],
                        CH9S = tmpList[17],
                        CH10M = tmpList[18],
                        CH10S = tmpList[19],
                        CH11M = tmpList[20],
                        CH11S = tmpList[21],
                        CH12M = tmpList[22],
                        CH12S = tmpList[23],
                        CH13M = tmpList[24],
                        CH13S = tmpList[25],
                        CH14M = tmpList[26],
                        CH14S = tmpList[27],
                        CH15M = tmpList[28],
                        CH15S = tmpList[29],
                        CH16M = tmpList[30],
                        CH16S = tmpList[31]
                    };
                    temperaList.Add(tempera);
                }
            }
            //
            List<PressureRecord> pressureRecords = new List<PressureRecord>();
            PressureRecord record = new PressureRecord();
            foreach (var item in TowdicRow)
            {
                foreach (var tmp in item.Value)
                {
                    record = new PressureRecord()
                    {
                        FID = Guid.NewGuid().ToString(),
                        VerInfoID = vi.FID,
                        DicCode = item.Key,
                        DicCode2 = "",
                        DicCode3 = "",
                        RecordTime = tmp.Key,
                        RecordVal = tmp.Value
                    };
                    pressureRecords.Add(record);
                }
            }
            #endregion

            #region 保存到数据库

            #endregion
            // 需要保存的对象以及集合 vi,temperaList,pressureRecords

           SaveData(vi,temperaList,pressureRecords);
          
        }
        /// <summary>
        /// STA文件解析
        /// </summary>
        /// <param name="row"></param>
        void STAAnalysis(string[] row)
        {
            #region 读取STA信息
            // 
            string[] verInfo1 = row[0].Split(new string[] { "," }, StringSplitOptions.None);    // 版本信息
            string[] verInfo2 = row[1].Split(new string[] { "," }, StringSplitOptions.None);    // 版本信息 
            // 
            string[] DataR = row.Skip(2).Take(32).ToArray();                                    //
            string[] DataR2 = row.Skip(34).Take(12).ToArray();
            // 
            string[] DataR3 = row.Skip(46).Take(3).ToArray();
            string[] DataR4 = row.Skip(49).Take(1).ToArray();

            string CoolingValve = "CoolingValve";
            string MoldTemperature = "MoldTemperature";
            string PressurePoint = "PressurePoint";
            string PressurePointEnd = "PressurePointEnd";

            #endregion

            #region 冷却阀
            string[] code = { "",
                "ControlSelection",
                "ControlTemperatureCh",
                "TemperatureControlRangeSelection",
                "FBOneTargetTemperature",
                "FBOneCoolingWait",
                "FBOneCoolingTime",
                "FBTwoTargetTemperature",
                "FBTwoCoolingWait",
                "FBTwoCoolingTime",
                "FBThreeTargetTemperature",
                "FBThreeCoolingWait",
                "FBThreeCoolingTime",
                "SEQOneCoolingWait",
                "SEQOneCoolingTime",
                "SEQTwoCoolingWait",
                "SEQTwoCoolingTime",
                "SEQThreeCoolingWait",
                "SEQThreeCoolingTime"
            };
            Dictionary<string, Dictionary<int, Dictionary<string, string>>> dic = new Dictionary<string, Dictionary<int, Dictionary<string, string>>>();
            Dictionary<int, Dictionary<string, string>> dicList = new Dictionary<int, Dictionary<string, string>>();
            Dictionary<string, string> pairs = null;
            string[] tmpList = null;
            foreach (var tmp in DataR)
            {
                tmpList = tmp.Split(new string[] { "," }, StringSplitOptions.None);
                pairs = new Dictionary<string, string>();
                int i = 0;
                foreach (var item in tmpList)
                {
                    i++;
                    pairs.Add(code[i], item);
                }
                dicList.Add(dicList.Count + 1, pairs);
            }
            dic.Add(CoolingValve, dicList);
            #endregion

            #region 模具温度
            string[] code2 = { "",
                "MonitoringWhetherOrNot",
                "UpperMonitoringLimit",
                "MonitoringLowerLimit",
                "TwoMonitoringWhetherOrNot",
                "TwoUpperMonitoringLimit",
                "TwoMonitoringLowerLimit"
            };
            dicList = new Dictionary<int, Dictionary<string, string>>();
            foreach (var tmp in DataR2)
            {
                tmpList = tmp.Split(new string[] { "," }, StringSplitOptions.None);
                pairs = new Dictionary<string, string>();
                int i = 0;
                foreach (var item in tmpList)
                {
                    i++;
                    pairs.Add(code2[i], item);
                }
                dicList.Add(dicList.Count + 1, pairs);
            }
            dic.Add(MoldTemperature, dicList);
            #endregion

            #region 加压点
            string[] Colcode = { "", "P1", "P2", "P3" };
            string[] Colcode2 = { "", "End" };
            string[] code3 = { "", "TargetForce", "AddingTime" };
            dicList = new Dictionary<int, Dictionary<string, string>>();
            foreach (var tmp in DataR3)
            {
                tmpList = tmp.Split(new string[] { "," }, StringSplitOptions.None);
                pairs = new Dictionary<string, string>();
                int i = 0;
                foreach (var item in tmpList)
                {
                    i++;
                    pairs.Add(code3[i], item);
                }
                dicList.Add(dicList.Count + 1, pairs);
            }
            dic.Add(PressurePoint, dicList);


            //
            string[] code4 = { "", "AddingTime" };
            dicList = new Dictionary<int, Dictionary<string, string>>();
            foreach (var tmp in DataR4)
            {
                tmpList = tmp.Split(new string[] { "," }, StringSplitOptions.None);
                pairs = new Dictionary<string, string>();
                int i = 0;
                foreach (var item in tmpList)
                {
                    i++;
                    pairs.Add(code4[i], item);
                }
                dicList.Add(dicList.Count + 1, pairs);
            }
            dic.Add(PressurePointEnd, dicList);
            #endregion

            #region 开始形成Class
            // 
            VerInfo vi = new VerInfo()
            {
                // FID
                FID = Guid.NewGuid().ToString(),
                // 文件类型
                DSType = "STA",
                // 机号
                DeviceNum = DeviceNum,
                // 机种名
                MachineName = verInfo1[5],
                // 操作者NO
                CZRNO = verInfo1[6],
                // 二维码
                QR = verInfo1[1],
                // 数据收集时间
                CollectionTime = verInfo1[7],
                // 铸造加压时间(0.1s)
                StressTime = verInfo1[0],
                // 制品(shot）NO
                ProductsNo = verInfo1[2],
                // 数据保存  年月日
                YMD = verInfo1[3],
                // 数据保存  时分秒
                HMS = verInfo1[4],
                // 注汤前时间
                TTime = verInfo1[8],
                // 预备
                Prepare = verInfo1[9],
                // 溶汤温度
                DissolvingTemperature = verInfo1[10],
                // 铸造机加压时间
                HHStressTime = verInfo1[11],
                // 铸造机冷却时间
                HHCoolingTime = verInfo1[12],
                // 铸造机抽芯时间
                HHLooseCoreTime = verInfo1[13],
                // 最终加压未使用
                EndStressNotUsed = verInfo1[14],
                // S阀用下限值
                SValvesLower = verInfo2[0],
                // S阀用上限值
                SValvesUpper = verInfo2[1],
                // M阀用下限值
                MValvesLower = verInfo2[2],
                // M阀用上限值
                MValvesUpper = verInfo2[3],
                // 加压开始
                StressStart = verInfo2[4],
                // 加压中
                Stressing = verInfo2[5],
                // 据点以及应用程序版本NO
                AppVerNo = verInfo1[0],
            };

            // 
            List<TEMPerA> temperaList = new List<TEMPerA>();
            TEMPerA tempera = null;
            
            // 1
            Dictionary<string, string[]> keyValuePairs = new Dictionary<string, string[]>();

            foreach (var tmp in dic[CoolingValve])
            { 
                foreach (var item in tmp.Value) {
                    if (keyValuePairs.ContainsKey(item.Key))
                    {
                        List<string> ls =  keyValuePairs[item.Key].ToList();
                        ls.Add(item.Value);
                        keyValuePairs[item.Key] = ls.ToArray(); 
                    }
                    else {
                        keyValuePairs.Add(item.Key, new string[] { item.Value });
                    }
                } 
            }
            foreach (var tmp in keyValuePairs) {
                tempera = new TEMPerA()
                {
                    FID = Guid.NewGuid().ToString(),
                    VerInfoID = vi.FID,
                    DicCode = CoolingValve,
                    DicCode2 = tmp.Key,
                    DicCode3 = "",
                    CH1M = tmp.Value[0],
                    CH1S = tmp.Value[1],
                    CH2M = tmp.Value[2],
                    CH2S = tmp.Value[3],
                    CH3M = tmp.Value[4],
                    CH3S = tmp.Value[5],
                    CH4M = tmp.Value[6],
                    CH4S = tmp.Value[7],
                    CH5M = tmp.Value[8],
                    CH5S = tmp.Value[9],
                    CH6M = tmp.Value[10],
                    CH6S = tmp.Value[11],
                    CH7M = tmp.Value[12],
                    CH7S = tmp.Value[13],
                    CH8M = tmp.Value[14],
                    CH8S = tmp.Value[15],
                    CH9M = tmp.Value[16],
                    CH9S = tmp.Value[17],
                    CH10M = tmp.Value[18],
                    CH10S = tmp.Value[19],
                    CH11M = tmp.Value[20],
                    CH11S = tmp.Value[21],
                    CH12M = tmp.Value[22],
                    CH12S = tmp.Value[23],
                    CH13M = tmp.Value[24],
                    CH13S = tmp.Value[25],
                    CH14M = tmp.Value[26],
                    CH14S = tmp.Value[27],
                    CH15M = tmp.Value[28],
                    CH15S = tmp.Value[29],
                    CH16M = tmp.Value[30],
                    CH16S = tmp.Value[31]
                };
                temperaList.Add(tempera);
            }

            // 2
            Dictionary<string, string[]> keyValuePairs2 = new Dictionary<string, string[]>();
            foreach (var tmp in dic[MoldTemperature])
            { 

                foreach (var item in tmp.Value)
                {
                    if (keyValuePairs2.ContainsKey(item.Key))
                    {
                        List<string> ls = keyValuePairs2[item.Key].ToList();
                        ls.Add(item.Value);
                        keyValuePairs2[item.Key] = ls.ToArray();
                    }
                    else
                    {
                        keyValuePairs2.Add(item.Key, new string[] { item.Value });
                    }
                }
            }
            foreach (var tmp in keyValuePairs2)
            {
                tempera = new TEMPerA()
                {
                    FID = Guid.NewGuid().ToString(),
                    VerInfoID = vi.FID,
                    DicCode = MoldTemperature,
                    DicCode2 = tmp.Key,
                    DicCode3 = "",
                    CH1M = tmp.Value[0],
                    CH1S = tmp.Value[1],
                    CH2M = tmp.Value[2],
                    CH2S = tmp.Value[3],
                    CH3M = tmp.Value[4],
                    CH3S = tmp.Value[5],
                    CH4M = tmp.Value[6],
                    CH4S = tmp.Value[7],
                    CH5M = tmp.Value[8],
                    CH5S = tmp.Value[9],
                    CH6M = tmp.Value[10],
                    CH6S = tmp.Value[11],
                    CH7M = "",
                    CH7S = "",
                    CH8M = "",
                    CH8S = "",
                    CH9M = "",
                    CH9S = "",
                    CH10M = "",
                    CH10S = "",
                    CH11M = "",
                    CH11S = "",
                    CH12M = "",
                    CH12S = "",
                    CH13M = "",
                    CH13S = "",
                    CH14M = "",
                    CH14S = "",
                    CH15M = "",
                    CH15S = "",
                    CH16M = "",
                    CH16S = ""
                };
                temperaList.Add(tempera);
            }

            // 3
            List<PressureRecord> pressureRecords = new List<PressureRecord>();
            PressureRecord record = new PressureRecord();
            int dic3 = 0;
            foreach (var tmp in dic[PressurePoint])
            {
                dic3++;
                foreach (var item in tmp.Value) {
                    record = new PressureRecord()
                    {
                        FID = Guid.NewGuid().ToString(),
                        VerInfoID = vi.FID,
                        DicCode = PressurePoint,
                        DicCode2 = item.Key,
                        DicCode3 = Colcode[dic3],
                        RecordTime = "",
                        RecordVal = item.Value,
                    };
                    pressureRecords.Add(record);
                } 
            }

            // 4
            int dic4 = 0;
            foreach (var tmp in dic[PressurePointEnd])
            {
                dic4++;
                foreach (var item in tmp.Value)
                {
                    record = new PressureRecord()
                    {
                        FID = Guid.NewGuid().ToString(),
                        VerInfoID = vi.FID,
                        DicCode = PressurePoint,
                        DicCode2 = item.Key,
                        DicCode3 = Colcode2[dic4],
                        RecordTime = "",
                        RecordVal = item.Value,
                    };
                    pressureRecords.Add(record);
                }
            }
            #endregion

            // 需要保存的对象以及集合 vi,temperaList,pressureRecords

            #region 保存到数据库
            // 开始插入数据库
            SaveData(vi, temperaList, pressureRecords);
            #endregion 
        }

        void SaveData(VerInfo vi,List<TEMPerA> temperaList,List<PressureRecord> pressureRecords)
        {
            string sql = $"SELECT COUNT(*) FROM  VerInfo WHERE QR='{vi.QR}' AND DeviceNum='{vi.DeviceNum}' AND DSType='{vi.DSType}'";
            var count =Convert.ToInt32( MySqlHelper.ExecuteScalar(sql));

            if (count > 0)
            {
                return;
            }

            using (var conn = MySqlHelper.GetConnection())
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                var tran = conn.BeginTransaction();

                MySqlHelper.InsertWitTransation(vi, "VerInfo", tran);

                temperaList.ForEach(p =>
                {
                    try
                    {
                        MySqlHelper.InsertWitTransation(p, "TEMPerA", tran);
                    }
                    catch (Exception e)
                    {
                        LogHelper.Error(e);
                    }
                });

                pressureRecords.ForEach(p =>
                {
                    try
                    {
                        MySqlHelper.InsertWitTransation(p, "PressureRecord", tran);
                    }
                    catch (Exception e)
                    {
                        LogHelper.Error(e);
                    }

                });

                tran.Commit();

                LogHelper.Info($"【{vi.QR}】数据保存成功");
            }
        }

       
    }
}
