using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;
using COI.WangWei.Model;
using COI.WangWei.BLL;
using System.Threading;
using COI.Frameworks;
using LumiSoft.Net.IMAP;
using LumiSoft.Net.IMAP.Client;

namespace MailService
{
    class Main
    {
        public IMAP_Client Server;
        public string Data;
        public int totalAccount;
        public int totalEmail;
        DateTime? lastGetTime;
        string server;
        public void Recive(object obj,System.Timers.ElapsedEventArgs e)
        {
            totalAccount = 0;
            totalEmail = 0;
           
            List<BrandInfo> all = new BrandBLL().GetBrands("", "", "", "", "", "", "", "","", null);
            UniEdit.writeLog("开始读取邮件，时间:" + DateTime.Now.ToString());

            //循环获取邮件
            for (int k=0;k<all.Count;k++ )
            {          
                lastGetTime = (all[k].EditTime == null) ? DateTime.MinValue : all[k].EditTime;
                server = UniEdit.readserver(all[k].AccountNo);               
                login(all[k].AccountNo.Replace("@" + server + ".com", ""), all[k].EPassword.Trim());//登陆               
                retrive(all[k].AccountNo,"INBOX");//读取收件箱
                retrive(all[k].AccountNo, "已发送");//读取发件箱
                all[k].EditTime = DateTime.Now;
            }

            UniEdit.writeLog("共读取" + totalAccount + "个账户" + totalEmail + "份邮件");
            UniEdit.writeLog("结束读取邮件，时间：\r\n" + DateTime.Now.ToString());
        }
        private void login(string user, string pass)
        {
            try
            {
                Server = new IMAP_Client();
                Server.Connect("imap." + server + ".com", 143);
                Server.Authenticate(user, pass);
                totalAccount++;
            }
            catch 
            {
                UniEdit.writeLog(user + " login failed " + DateTime.Now.ToString());                
            }

        }
        private void retrive(string account,string box)
        {
            string[] szTemp;
            string htmlstr = "";
            int k ;
         
            Server.SelectFolder(box);//选择邮箱文件夹
            int Count = Server.MessagesCount;//获取文件夹内邮件数目
            LumiSoft.Net.IMAP.IMAP_SequenceSet e = new LumiSoft.Net.IMAP.IMAP_SequenceSet();
            IMAP_FetchItem_Flags r = IMAP_FetchItem_Flags.All;//获取邮件所有信息
            IMAP_FetchItem[] w;

            //逐封读取邮件
            for (; Count > 0; Count--)
            {
                try
                {
                    EmailInfo email = new EmailInfo();
                    email.Attachs = new List<EmailAttachInfo>();
                    Common mail = new Common();

                    e.Parse(Count.ToString() + ":" + Count.ToString());//获取一封
   
                    w = Server.FetchMessages(e, r, false, false);
     
                    string data = Encoding.Default.GetString(w[0].MessageData);
                    email.MailTime = w[0].Envelope.Date;

                    if (email.MailTime < lastGetTime)
                    {
                        break;
                    }

                    szTemp = data.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.None); //每行放进一个数组元素中
                    szTemp[szTemp.Length - 1] = ".";//最后一行添加标识符"."
                    k = 0;//行数

                    while (szTemp[k] != ".")
                    {
                        //不断地读取邮件内容 
                        mail.Content += szTemp[k] + "\n";
                        k++;

                        if (szTemp[k] != "")
                        {
                            mail.readBounChar(szTemp[k]);//在邮件头中获取charset与boundary
                        }

                        //正文
                        //无boundary分界符
                        if (mail.bounmark == 0 && szTemp[k] == "")
                        {
                            while (szTemp[k] != "")
                            {
                                mail.Content += szTemp[k] + "\n";
                                k++;
                            }
                            while (szTemp[k] == "")
                            {
                                mail.Content += szTemp[k] + "\n";
                                k++;
                            }
                            if (mail.contentEncoding.ToLower() == "quoted-printable")
                            {
                                while (!(szTemp[k].Contains("_Part_") || szTemp[k].Contains("_NextPart_")) && szTemp[k] != ".")
                                {
                                    mail.BuilderTemp.Append(mail.QPHandler(szTemp[k]));
                                    k++;
                                    mail.Content += szTemp[k] + "\n";
                                }
                            }
                            else
                            {
                                while (!(szTemp[k].Contains("_Part_") || szTemp[k].Contains("_NextPart_")) && szTemp[k] != ".")
                                {
                                    mail.BuilderTemp.Append(szTemp[k]);
                                    k++;
                                    mail.Content += szTemp[k] + "\n";
                                }
                            }

                            htmlstr += mail.contentDecode();
                            mail.Content += szTemp[k] + "\n";
                        }

                        //有boundary分界符
                        else
                        {
                            if (szTemp[k] == "--" + mail.boundary[mail.bounmark])
                            {
                                while (mail.bounmark > 0)
                                {
                                    while (szTemp[k] != "")
                                    {
                                        mail.Content += szTemp[k] + "\n";
                                        k++;
                                        mail.readBounHeader(szTemp[k]);
                                    }
                                    while (szTemp[k] == "")
                                    {
                                        mail.Content += szTemp[k] + "\n";
                                        k++;
                                    }
                                    mail.Content += szTemp[k] + "\n";
                                    if (mail.contentEncoding.ToLower() == "quoted-printable")
                                    {
                                        while (szTemp[k] != "--" + mail.boundary[mail.bounmark] + "--" && szTemp[k] != "--" + mail.boundary[mail.bounmark])
                                        {
                                            mail.BuilderTemp.Append(mail.QPHandler(szTemp[k]));
                                            k++;
                                            mail.Content += szTemp[k] + "\n";
                                        }
                                    }
                                    else
                                    {
                                        while (szTemp[k] != "--" + mail.boundary[mail.bounmark] + "--" && szTemp[k] != "--" + mail.boundary[mail.bounmark])
                                        {
                                            mail.BuilderTemp.Append(szTemp[k]);
                                            k++;
                                            mail.Content += szTemp[k] + "\n";
                                        }
                                    }
                                    if (szTemp[k] == "--" + mail.boundary[mail.bounmark] + "--")
                                    {
                                        mail.bounmark--;
                                    }
                                    //附件
                                    if (mail.filename != "")
                                    {
                                        byte[] attachFile = Convert.FromBase64String(mail.BuilderTemp.ToString());
                                        mail.url = "..\\" + account + "\\" + DateTime.Now.ToFileTime().ToString();
                                        Directory.CreateDirectory(mail.url);
                                        mail.url += "\\" + mail.filename;
                                        FileStream fs = new FileStream(mail.url, FileMode.CreateNew);
                                        BinaryWriter bw = new BinaryWriter(fs);
                                        bw.Write(attachFile);
                                        bw.Close();
                                        fs.Close();
                                        email.Attachs.Add(new EmailAttachInfo(0, mail.filename, email.Id, mail.url));
                                        mail.BuilderTemp.Clear();
                                        mail.filename = "";
                                    }
                                    htmlstr += mail.contentDecode();
                                    mail.Content += szTemp[k] + "\n";
                                }
                            }
                        }
                    }

                    //储存到数据库
                    email.Content = htmlstr;
                    htmlstr = "";
                    email.Subject = w[0].Envelope.Subject == null ? "(无主题)" : w[0].Envelope.Subject;
                    email.From = w[0].Envelope.From[0].ToString();
                    email.To = w[0].Envelope.To[0].ToString();
                    email.Email = account;
                    email.GetTime = DateTime.Now;

                    email.Category = new CodeInfo(box == "INBOX" ? "0601" : "0602", "");
                    new EmailBLL().Edit(email, 1, new EmployeeInfo());
                    totalEmail++;
                }
                catch(Exception err)
                {
                    UniEdit.writeLog("retrive failed: mail:" +account+" "+box + "#"
                        + Count.ToString() + "\r\n" + err.ToString() + "\r\n\r\n");
                }

                //断开连接
                Server.Disconnect();
                Server.Dispose();
            }
        }     
    }
}
