﻿using DATFileReader.Helper;
using DATFileReader.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows.Forms;
using System.Data.Linq;
using System.Linq;
using System.Threading.Tasks;
using DataAccess;
using DataAccess.CustomEnums;
using DATFileReader.Repository;
using hn.Common;

namespace DATFileReader
{
    public partial class FrmMain : Form
    {


        #region 窗体
        public FrmMain()
        {
            InitializeComponent();

        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            Init();
        }
        #endregion

        #region 定义初始化公共变量
        // 是否运行 
        VerInfoRepository verInfoRepository = new VerInfoRepository();
        TEMPerARepository temPerARepository = new TEMPerARepository();
        PressureRepository pressureRepository = new PressureRepository();
        private bool isRunning { get; set; }                                                                            // 是否执行中
        string DeviceNum = ConfigurationManager.AppSettings["DeviceNum"].ToString();                                    // 几号机
        string scanInterval = ConfigurationManager.AppSettings["ScanInterval"];                                         // 定时秒数 
        private bool SD = false;                    // 是否点击采集过
        Timer timerInit = null;                     // 定时点击开始采集
        Timer timer = null;                         // 定时采集 
        List<string> EdList = new List<string>();   // 已经插入到数据库的文件
        // 开始采集定时器
        int sm = 1;
        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        void Init()
        {
            if (string.IsNullOrWhiteSpace(DeviceNum))
            {
                if (InputDialog.Show("请输入机台号", out DeviceNum).Equals(DialogResult.OK))
                {
                    SaveConfig("DeviceNum", DeviceNum);
                    ConfigurationManager.RefreshSection("appSettings");
                }
                else
                {
                    Application.Exit();
                }
            }
            if (string.IsNullOrWhiteSpace(scanInterval)) { MessageBox.Show("请设置定时扫描秒数!"); Application.Exit(); }
            // 
            numInterval.Value = Convert.ToInt32(scanInterval) / 1000;
            LogHelper.Init(new TextBoxWriter(txtLog));
            txtPath.Text = ConfigurationManager.AppSettings["dirPath"] != null ? ConfigurationManager.AppSettings["dirPath"].ToString() : "";
            if (!(string.IsNullOrWhiteSpace(txtPath.Text)))
            {
                btnStart.Visible = true;
                if (!SD)
                {
                    timerInit = new Timer();
                    timerInit.Interval = 10000;
                    timerInit.Tick += btnStart_Click;
                    timerInit.Start();
                    WindowState = FormWindowState.Minimized;
                }
            }
            //if (!MySqlHelper.IsOpen())
            //{
            //    LogHelper.Info($"网络连接失败!");
            //}
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
                    btnStart.Visible = true;
                }
            }
        }
        /// <summary>
        /// 开始采集
        /// </summary>
        private void btnStart_Click(object sender, EventArgs e)
        {
            EdList=new List<string>();
            if (timerInit != null)
            {
                timerInit.Enabled = false;
                timerInit.Stop();
                timerInit = null;
            }
            SD = true;  // 表示点击过开始采集
            if (timer == null)
            {
                timer = new Timer();
            }
            if (btnStart.Text == "开始采集")
            {
                // 保存到文件
                SaveConfig("ScanInterval", (numInterval.Value * 1000).ToString());
                SaveConfig("dirPath", txtPath.Text.ToString());
                ConfigurationManager.RefreshSection("appSettings");

                timer.Interval = Convert.ToInt32(numInterval.Value * 1000);
                timer.Tick += Timer_Tick;
                btnStart.Text = "结束采集";
                btnPath.Visible = false;
                timer.Start();
                timer.Enabled = true;
                LogHelper.Info("开始采集");
            }
            else
            {
                btnStart.Text = "开始采集";
                btnPath.Visible = true;
                timer.Stop();
                timer.Enabled = false;
                LogHelper.Info("结束采集");
            }
        }
        /// <summary>
        /// 定时器
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (sm % 100 == 0 && sm >= 100)
                {
                    txtLog.Text = "";
                }
                if (sm == int.MaxValue)
                {
                    sm = 0;
                }
                LogHelper.Info($"定时扫描(次数)：" + sm);
                TimerSave();
                sm++;
            }
            catch (Exception ex)
            {
                LogHelper.Info($"定时解析异常：" + ex.Message);
            }
        }
        /// <summary>
        /// 定时解析保存
        /// </summary>
        void TimerSave()
        {
            var dirPath = txtPath.Text;
            if (!isRunning)
            {
                var task = new Task(() =>
                {
                    isRunning = true;
                    try
                    {
                        var filesName = DatFileReader.GetFiles(dirPath, new List<string>());

                        foreach (var f in filesName)
                        {
                            // 过滤已经插入到数据库
                            //if (EdList.Any(a => a.Equals(f))) { continue; }
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
                            //EdList.Add(f);
                        }  
                    }
                    catch (Exception exception)
                    {
                        LogHelper.Error(exception);
                    }
                    finally
                    {
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

            var compares = new Dictionary<string, CompareEnum>();
            compares.Add("QR", CompareEnum.Equal);
            var vi = verInfoRepository.GetSingle(new {QR = tmpInfo[1]}, compares);

            if (vi == null)
            {
                vi = new VerInfo { FID = Guid.NewGuid().ToString(), DSType = "DAT" };
            }
            else if (vi.DSType != "DAT")
            {
                vi.DSType = "true";
            }
            else if (vi.DSType == "true") {
                return;
            }
            else
            {
                return;
            }

            vi.AppVerNo = tmpInfo[0];
            vi.QR = tmpInfo[1];
            vi.CollectionTime = tmpInfo[2];
            vi.PressureDuration = tmpInfo[3];
            vi.DeviceNum = DeviceNum;
            #region 开始形成Class

            List<TEMPerA> temperaList = new List<TEMPerA>();
            TEMPerA tempera = null;
            List<string> tmpStringList = new List<string>();
            string[] tmpList = null; 

            foreach (var item in dic)
            {
                foreach (var tmp in item.Value)
                {
                    tmpList = tmp.Value.Split(new string[] { "," }, StringSplitOptions.None);
                    tmpStringList.AddRange(tmpList);
                    for (int k = 0; k < 32 - tmpList.Length; k++)
                    {
                        tmpStringList.Add("");
                    } 
                    tmpList = tmpStringList.ToArray();
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

            SaveData(vi, temperaList, pressureRecords);

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
            var compares = new Dictionary<string, CompareEnum>();
            compares.Add("QR", CompareEnum.Equal);
            var vi = verInfoRepository.GetSingle(new { QR = verInfo1[1] }, compares);
            if (vi == null)
            {
                vi = new VerInfo() { FID = Guid.NewGuid().ToString() };
                vi.DSType = "STA";
            }
            else if (vi.DSType != "STA")
            {
                vi.DSType = "true";
            }
            else if (vi.DSType == "true")
            {
                return;
            }
            else
            {
                return;
            }

            vi.DeviceNum = DeviceNum;
            vi.MachineName = verInfo1[5];
            vi.CZRNO = verInfo1[6];
            vi.QR = verInfo1[1];
            vi.CollectionTime = verInfo1[7];
            vi.StressTime = verInfo1[11];
            vi.ProductsNo = verInfo1[2];
            // 数据保存  年月日
            vi.YMD = verInfo1[3];
            // 数据保存  时分秒
            vi.HMS = verInfo1[4];
            // 注汤前时间
            vi.TTime = verInfo1[8];
            // 预备
            vi.Prepare = verInfo1[9];
            // 溶汤温度
            vi.DissolvingTemperature = verInfo1[10];
            // 铸造机加压时间
            vi.HHStressTime = verInfo1[11];
            // 铸造机冷却时间
            vi.HHCoolingTime = verInfo1[12];
            // 铸造机抽芯时间
            vi.HHLooseCoreTime = verInfo1[13];
            // 最终加压未使用
            vi.EndStressNotUsed = verInfo1[14];
            // S阀用下限值
            vi.SValvesLower = verInfo2[0];
            // S阀用上限值
            vi.SValvesUpper = verInfo2[1];
            // M阀用下限值
            vi.MValvesLower = verInfo2[2];
            // M阀用上限值
            vi.MValvesUpper = verInfo2[3];
            // 加压开始
            vi.StressStart = verInfo2[4];
            // 加压中
            vi.Stressing = verInfo2[5];
            // 据点以及应用程序版本NO
            vi.AppVerNo = verInfo1[0];

            // 
            List<TEMPerA> temperaList = new List<TEMPerA>();
            TEMPerA tempera = null;

            // 1
            Dictionary<string, string[]> keyValuePairs = new Dictionary<string, string[]>();

            foreach (var tmp in dic[CoolingValve])
            {
                foreach (var item in tmp.Value)
                {
                    if (keyValuePairs.ContainsKey(item.Key))
                    {
                        List<string> ls = keyValuePairs[item.Key].ToList();
                        ls.Add(item.Value);
                        keyValuePairs[item.Key] = ls.ToArray();
                    }
                    else
                    {
                        keyValuePairs.Add(item.Key, new string[] { item.Value });
                    }
                }
            }
            foreach (var tmp in keyValuePairs)
            {
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
                foreach (var item in tmp.Value)
                {
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
        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="vi">表头</param>
        /// <param name="temperaList">明细数据</param>
        /// <param name="pressureRecords">加压记录</param>
        void SaveData(VerInfo vi, List<TEMPerA> temperaList, List<PressureRecord> pressureRecords)
        {
            #region 查询是否有该条记录
            var viwhere = new VerInfo()
            {
                QR = vi.QR,
                DeviceNum = vi.DeviceNum,
                DSType = "true"
            };
            var dic = new Dictionary<string, CompareEnum>();
            dic.Add("QR", CompareEnum.Equal);
            dic.Add("DeviceNum", CompareEnum.Equal);
            dic.Add("DSTYPE", CompareEnum.Equal);
            var count = verInfoRepository.Select(viwhere, dic).Count();

            if (count > 0)
            {
                return;
            }
            #endregion

            var startTime = DateTime.Now;

            #region 组装数据
            var targetList = pressureRecords.Where(p => p.DicCode == "TargetPressureForPressurization").AsParallel();
            var pressauList = pressureRecords.Where(p => p.DicCode == "PressureOfPressurizedState").AsParallel();
            var outputList = pressureRecords.Where(p => p.DicCode == "LevelOutputValueForPressurization").AsParallel();

            foreach (var record in pressauList)
            {
                var targetRow = targetList.FirstOrDefault(p => p.VerInfoID == record.VerInfoID && p.RecordTime == record.RecordTime);
                var outputRow = outputList.FirstOrDefault(p => p.VerInfoID == record.VerInfoID && p.RecordTime == record.RecordTime);

                record.OutputVal = outputRow is null ? null : outputRow.RecordVal;
                record.TargetVal = targetRow is null ? null : targetRow.RecordVal;
            }

            pressureRecords = pressauList.ToList();
            #endregion

            // 开始事务
            using (var conn = DBConnectionFactory.GetConnection(DBTypeEnums.MYSQL))
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                var tran = conn.BeginTransaction();

                if (vi.DSType == "true")
                {
                    verInfoRepository.Update(vi, new { QR = vi.QR }, tran);
                }
                else
                {
                    verInfoRepository.Insert(vi, tran);
                }

                temPerARepository.InsertBulk(temperaList, tran);
                pressureRepository.InsertBulk(pressureRecords, tran);

                //for (int i = 0; i < (int)Math.Ceiling((decimal)temperaList.Count / tCount); i++)
                //{
                //    temPerARepository.InsertBulk(temperaList.Skip(i * tCount).Take(tCount).ToList());
                //}

                //for (int i = 0; i < (int)Math.Ceiling((decimal)pressureRecords.Count / tCount); i++)
                //{
                //    pressureRepository.InsertBulk(pressureRecords.Skip(i * tCount).Take(tCount).ToList());
                //}


                tran.Commit();
                
            }
            //
            var timespan = DateTime.Now - startTime;
            try
            { 
                MySqlHelper.ExecuteNonQueryStoredProcedure($"ver_qr", new MySql.Data.MySqlClient.MySqlParameter[] { new MySql.Data.MySqlClient.MySqlParameter("str", vi.QR) });
                LogHelper.Info($"【{vi.QR}】数据保存成功，数据写入耗时【{timespan.TotalSeconds}】秒");
            }
            catch (Exception ex)
            {
                LogHelper.Info($"【{vi.QR}】数据call ver_qr('{vi.QR}')异常【{ex.Message}】");
            }
        }
        /// <summary>
        /// 保存Config
        /// </summary>
        private void SaveConfig(string key, string value)
        {
            ExeConfigurationFileMap ecf = new ExeConfigurationFileMap();
            ecf.ExeConfigFilename = System.Windows.Forms.Application.StartupPath + "\\" + "DATFileReader.exe.config";   // 获取上一次保存的设置
            Configuration config = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(ecf, ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[key] != null)
            {
                config.AppSettings.Settings[key].Value = value;
            }
            else
            {
                config.AppSettings.Settings.Add(key, value);
            }

            config.Save(ConfigurationSaveMode.Modified);
        }



        //
        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            //if (MessageBox.Show("是否确认退出程序？", "退出", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            //{
            //    // 关闭所有的线程
            //    this.Dispose();
            //    this.Close();
            //}
            //else
            //{
            //    e.Cancel = true;
            //}
            WindowState = FormWindowState.Minimized;
            e.Cancel = true;
           
        }

        private void FrmMain_SizeChanged(object sender, EventArgs e)
        {
            //判断是否选择的是最小化按钮
            if (WindowState == FormWindowState.Minimized)
            {
                //隐藏任务栏区图标
                this.ShowInTaskbar = false;
                //图标显示在托盘区
                notifyIcon1.Visible = true;
            }
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
                this.notifyIcon1.Visible = false;
                this.ShowInTaskbar = true;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                //还原窗体显示    
                WindowState = FormWindowState.Normal;
                //激活窗体并给予它焦点
                this.Activate();
                //任务栏区显示图标
                this.ShowInTaskbar = true;
                //托盘区图标隐藏
                notifyIcon1.Visible = false;
            }
        }
    }
}
