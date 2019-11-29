using DATFileReader.WinDialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DATFileReader.Helper
{
    public static class InputDialog
    {
        public static DialogResult Show(string FrmText,out string strText)
        {
            string strTemp = string.Empty;

            FrmInputDialog inputDialog = new FrmInputDialog();
            inputDialog.Text = FrmText;
            inputDialog.TextHandler = (str) => { strTemp = str; };

            DialogResult result = inputDialog.ShowDialog();
            strText = strTemp;

            return result;
        }
    }
}
