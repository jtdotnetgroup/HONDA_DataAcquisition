using System.Collections.Generic;
using System.IO;

namespace DATFileReader
{
    public class DatFileReader
    {
        public static string Open(string filePath)
        {
            FileStream fs=new FileStream(filePath,FileMode.Open);
            StreamReader sr=new StreamReader(fs);

            string result = sr.ReadToEnd();
            sr.Dispose();
            fs.Dispose();
            return result;

        }

        public static List<string> ScanDir(string dirPath)
        {
            var dir = new DirectoryInfo(dirPath);
            var files = dir.GetFiles();
            var result = new List<string>();

            foreach (var f in files)
            {
                result.Add(f.FullName);
            }

            return result;
        }
    }
}