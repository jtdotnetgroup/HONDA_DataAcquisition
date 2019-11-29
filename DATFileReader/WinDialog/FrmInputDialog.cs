using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DATFileReader.WinDialog
{
    /// <summary>
    /// https://www.cnblogs.com/hjsstudio/p/9676111.html  
    /// </summary>
    public partial class FrmInputDialog : Form
    {
        public FrmInputDialog()
        {
            InitializeComponent();
        }
        public delegate void TextEventHandler(string strText);
        public TextEventHandler TextHandler;
        private void btnOK_Click(object sender, EventArgs e)
        {
            if (null != TextHandler)
            {
                TextHandler.Invoke(txtString.Text);
                DialogResult = DialogResult.OK;
            }
        }

        private void txtString_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Keys.Enter == (Keys)e.KeyChar)
            {
                if (null != TextHandler)
                {
                    TextHandler.Invoke(txtString.Text);
                    DialogResult = DialogResult.OK;
                }
            }
        }
    }
}
