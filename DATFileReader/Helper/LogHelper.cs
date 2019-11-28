using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using log4net;
using log4net.Core;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace hn.Common
{
    public class LogHelper
    {
        private static readonly ILog InfoLogger = LogManager.GetLogger("INFO");
        private static readonly ILog ErrorLogger = LogManager.GetLogger("ERROR");
        static object lockobj = new object();
        private static TextWriter _textWriter;

        public static void Info(string msg)
        {
            lock (lockobj)
            {
                InfoLogger.Info(msg);
            }
        }

        public static void Error(Exception ex)
        {
            lock (lockobj)
            {
                ErrorLogger.Error(ex);
            }
        }

        public static void Error(string ex)
        {
            lock (lockobj)
            {
                ErrorLogger.Error(ex);
            }
        }

        public static void WriteLog(string msg)
        {
            Info(msg);
        }

        public static void WriteLog(Exception ex)
        {
            Error(ex);
        }

        public static void WriteLog(Type type, Exception ex)
        {
            Error(ex);
        }

        public static void WriteLog(Type type, string ex)
        {
            Error(ex);
        }

        public static void Init(TextWriter writer)
        {
            _textWriter = writer;
            Console.SetOut(_textWriter);
        }
    }



    public class TextBoxWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        private TextBox textBox;
        delegate void WriteFunc(string value);
        WriteFunc write;
        WriteFunc writeLine;

        public TextBoxWriter(TextBox text)
        {
            this.textBox = text;
            write = Write;
            writeLine = WriteLine;
        }

        public override void Write(string value)
        {
            if (textBox.InvokeRequired)
                textBox.BeginInvoke(write, value);
            else
                textBox.AppendText(value);
        }

        public override void WriteLine(string value)
        {
            if (textBox.InvokeRequired)
                textBox.BeginInvoke(writeLine, value);
            else
            {
                textBox.AppendText(value);
                textBox.AppendText(this.NewLine);
            }

        }
    }
}