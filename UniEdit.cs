using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;


namespace MailService
{
    public class UniEdit
    {
        public static string QPDecode(string contents, Encoding encoding)
        {
            const string QpSinglePattern = "(\\=([0-9A-F][0-9A-F]))";
            const string QpMutiplePattern = @"((\=[0-9A-F][0-9A-F])+=?\s*)+";
            if (contents == null)
            {
                throw new ArgumentNullException("contents");
            }
            // 替换被编码的内容
            string result = Regex.Replace(contents, QpMutiplePattern, new MatchEvaluator(delegate(Match m)
            {
                List<byte> buffer = new List<byte>();
                // 把匹配得到的多行内容逐个匹配得到后转换成byte数组
                MatchCollection matches = Regex.Matches(m.Value, QpSinglePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                foreach (Match match in matches)
                {
                    buffer.Add((byte)HexToByte(match.Groups[2].Value.Trim()));
                }
                return encoding.GetString(buffer.ToArray());
            }), RegexOptions.IgnoreCase | RegexOptions.Compiled);
            // 替换多余的链接=号
            result = Regex.Replace(result, @"=\s+", "");
            return result;
        }
        private static int HexToByte(string hex)
        {
            int num1 = 0;
            string text1 = "0123456789ABCDEF";
            for (int num2 = 0; num2 < hex.Length; num2++)
            {
                if (text1.IndexOf(hex[num2]) == -1)
                {
                    return -1;
                }
                num1 = (num1 * 0x10) + text1.IndexOf(hex[num2]);
            }
            return num1;
        }
        public static string HeadUnEncode(string source)
        {
            int startindex;
            int endindex; 
            Encoding charset;
            string code;
            string transfer;
            startindex = source.IndexOf("=?");
            endindex = source.IndexOf("?", startindex + 2);
            charset = Encoding.GetEncoding(source.Substring(startindex + 2, endindex - startindex - 2));
            startindex = endindex;
            transfer = source.Substring(startindex + 1, 1).ToLower();
            startindex = source.IndexOf("?", startindex + 1);
            endindex = source.IndexOf("?=", startindex+1);
            code = source.Substring(startindex + 1, endindex - startindex - 1);
            if (transfer == "b")
            {
                return charset.GetString(Convert.FromBase64String(code));
            }
            else
            {
                return QPDecode(code, charset);
            }
        }
        public static void writeLog(string source)
        {
            //FileStream fs = new FileStream("..\\log.txt", FileMode.Append);
            FileStream fs = new FileStream("c:\\log.txt", FileMode.Append);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(source);
            sw.Close();
            fs.Close();
        }
        /*public static DateTime readLog()
        {
            //FileStream fs = new FileStream("..\\log.txt", FileMode.OpenOrCreate);
            FileStream fs = new FileStream("c:\\log.txt", FileMode.OpenOrCreate);
            StreamReader rd = new StreamReader(fs);
            List<string> ls = new List<string>();
            DateTime dt;
            while (!rd.EndOfStream)
            {
                ls.Add(rd.ReadLine());
            }
            try
            {
                if (ls[ls.Count - 1]=="service stoped")
                {
                    dt = DateTime.Parse(ls[ls.Count - 2]);
                }
                else
                {
                    dt = DateTime.Parse(ls[ls.Count - 1]);
                }                
            }
            catch
            {
                dt = DateTime.MinValue;
            }                
            rd.Close();
            fs.Close();
            return dt;
        }*/
        public static string readserver(string source)
        {
            int start=source.IndexOf("@");
            int end = source.IndexOf(".");
            return source.Substring(start+1, end - start-1);
        }
    }
}
