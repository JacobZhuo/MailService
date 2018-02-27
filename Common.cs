using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;


namespace MailService
{
    class Common
    {        
        public string MailFrom = "";
        public string MailTo = "";
        public string MailTitle = "";
        public string MailDate = "";
        public string Content="";
        public string contentChars = "";
        public string contentEncoding = "";
        public string filename = "";
        public string url = "";
        public string[] boundary = new string[20];
        public int bounmark = 0;
        public StringBuilder BuilderTemp = new StringBuilder();        
        public int iftitle;
        private string temp;
        string htmlstr;
        public string contentType = "";

        private string IndexHeader(string header)
        {
            int headerstart = temp.ToLower().IndexOf(header);
            if (headerstart >= 0)
            {
                string a = temp.Substring(headerstart + header.Length+1, temp.Length - headerstart - header.Length-1);
                a = Regex.Replace(a, "[\";]", "").Trim();
                if (a.Contains("=?"))
                {
                    return UniEdit.HeadUnEncode(a);
                }
                else
                    return a;
            }
            else
                return "";
        }
        public string contentDecode()
        {
            if (contentType.ToLower().Contains("plain"))
            {
                BuilderTemp.Clear();
                return "";
            }
            else
            {
                if (contentEncoding.ToLower() == "base64")
                {
                    htmlstr = Encoding.GetEncoding(contentChars).GetString(Convert.FromBase64String(BuilderTemp.ToString()));
                    BuilderTemp.Clear();
                }
                else if (contentEncoding.ToLower() == "quoted-printable")
                {
                    htmlstr = UniEdit.QPDecode(BuilderTemp.ToString(), Encoding.GetEncoding(contentChars));
                    BuilderTemp.Clear();
                }
                else if (contentChars != "")
                {
                    htmlstr = Encoding.GetEncoding(contentChars).GetString(Encoding.ASCII.GetBytes(BuilderTemp.ToString()));
                    BuilderTemp.Clear();
                }
                else
                {
                    htmlstr = BuilderTemp.ToString();
                }
                return htmlstr;
            }
        }
        public void readBounHeader(string source)
        {
            temp = source;
     
                contentChars = IndexHeader("charset")==""?contentChars:IndexHeader("charset");

                contentEncoding = IndexHeader("content-transfer-encoding") == "" ? contentEncoding : IndexHeader("content-transfer-encoding");
              
                if (source.Contains("boundary=\""))
                {
                    int bounstart = 0;
                    int bounend = 0;
                    bounstart = source.IndexOf("=\"");
                    bounend = source.IndexOf("\"", bounstart + 2);
                    bounmark++;
                    boundary[bounmark] = source.Substring(bounstart + 2, bounend - bounstart - 2);
                }
             filename=IndexHeader("filename=")==""?filename:IndexHeader("filename=");
             contentType = IndexHeader("content-type") == "" ? contentType : IndexHeader("content-type");
         }
        public string QPHandler(string source)
        {
            if (source != "")
            {
                if (source.Substring(source.Length - 1, 1) == "=")
                {
                    source= source.Substring(0, source.Length-1);
                }                
            }          
                return source;
        }
        public DateTime IndexDate(string source)
        {
            Regex r = new Regex(@",\s+(?<1>[^\s]+)\s(?<2>[^\s]+)\s(?<3>[^\s]+)\s(?<4>[^\s]+)");
            Match m = r.Match(source);
            string[] k = new string[5];
            int n = 0;
            string format = "dMMMyyyyHH:mm:ss";
            CultureInfo ci = CultureInfo.CreateSpecificCulture("zh-CN");
            DateTimeFormatInfo dtfi = ci.DateTimeFormat;
            DateTime datetime=new DateTime();
            dtfi.AbbreviatedMonthNames = new string[] { "Jan", "Feb", "Mar", 
                                                  "Apr", "May", "Jun", 
                                                  "Jul", "Aug", "Sep", 
                                                  "Oct", "Nov", "Dec", "" };
            dtfi.AbbreviatedMonthGenitiveNames = dtfi.AbbreviatedMonthNames;
            if (m.Success)
            {
                foreach (Group g in m.Groups)
                {
                    k[n] = g.Value;
                    n++;
                }
                string date = k[1].Replace("0", "") + k[2] + k[3] + k[4];
                try
                {
                    datetime = DateTime.ParseExact(date, format, ci);
                }
                catch
                {
                    return DateTime.MinValue;
                }         
            }
            return datetime;         
            }
        public void readBounChar(string source)
        {
            temp = source;
            if (source.Contains("boundary=\""))
            {
                int bounstart = 0;
                int bounend = 0;
                bounstart = source.IndexOf("=\"");
                bounend = source.IndexOf("\"", bounstart + 2);
                bounmark++;
                boundary[bounmark] = source.Substring(bounstart + 2, bounend - bounstart - 2);
            }
            iftitle = Regex.Replace(source, @"\s", "").ToLower().IndexOf("h=");
            if (iftitle != 0)
            {
                if (contentChars == "")
                {
                    contentChars = IndexHeader("charset");
                }
                if (contentEncoding == "")
                {
                    contentEncoding = IndexHeader("content-transfer-encoding");
                }
            }
        }
    }
}

