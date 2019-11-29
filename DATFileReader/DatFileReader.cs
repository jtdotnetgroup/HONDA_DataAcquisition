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
        // path是文件夹路径，FileList是存放文件搜索结果的列表
        public static List<string> GetFiles(string path, List<string> FileList)
        {
            string filename;
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] fil = dir.GetFiles();
            DirectoryInfo[] dii = dir.GetDirectories();
            foreach (FileInfo f in fil)
            {
                filename = f.FullName;
                //FileList.Add(filename);
                /*我这边是想获取视频文件，请忽略。
                if (filename.EndsWith("mp4") | filename.EndsWith("mkv") | filename.EndsWith("wmv") | filename.EndsWith("avi") | filename.EndsWith("iso"))
                {
                    FileList.Add(filename);
                }*/
                if (filename.EndsWith("dat"))
                {
                    FileList.Add(filename);
                }
            }
            //获取子文件夹内的文件列表，递归遍历  
            foreach (DirectoryInfo d in dii)
            {
                GetFiles(d.FullName, FileList);
            }
            return FileList;
        }
    }
}