using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace MailService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {    
            //设置调用主程序时间间隔
            UniEdit.writeLog("service started");
            System.Timers.Timer t = new System.Timers.Timer();            
            t.Interval = 1000*60*60*24;
            t.Elapsed += new System.Timers.ElapsedEventHandler(new Main().Recive);
            t.AutoReset = true;
            t.Enabled = true;
           // Timer ts;            
            //ts = new Timer(new TimerCallback(new Main().Recive), null, 10, 1000*60*10);
        }

        protected override void OnStop()
        {
            UniEdit.writeLog("service stoped");
        }

    }
}
