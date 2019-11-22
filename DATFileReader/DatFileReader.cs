using System.IO;

namespace DATFileReader
{
    public class DatFileReader
    {
        public string Open(string filePath)
        {
            FileStream fs=new FileStream(filePath,FileMode.Open);
            StreamReader sr=new StreamReader(fs);

            string result = sr.ReadToEnd();
            sr.Dispose();
            fs.Dispose();
            return result;

        }
    }
}