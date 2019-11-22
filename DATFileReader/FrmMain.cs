using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DATFileReader
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
        }

        private void btnPath_Click(object sender, EventArgs e)
        {
            //using (var ofd= new FolderBrowserDialog())
            //{
            //    if (ofd.ShowDialog() == DialogResult.OK)
            //    {
            //        ofd.Description = "请选择DAT文件所在目录";
            //    }
            //}

            using (var ofd=new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    DatFileReader reader=new DatFileReader();
                    var txt = reader.Open(ofd.FileName);
                    txtLog.Text += txt;
                }
            }
        }
    }
}
