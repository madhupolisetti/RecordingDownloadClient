using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RecordingDownloadClient
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }
        ApplicationController applicationObj = new ApplicationController();

        public void Start()
        {
            System.Threading.Thread startServiceThread = new System.Threading.Thread(new System.Threading.ThreadStart(applicationObj.Start));
            startServiceThread.Start();
        }
        protected override void OnStart(string[] args)
        {
            System.Threading.Thread startServiceThread = new System.Threading.Thread(new System.Threading.ThreadStart(applicationObj.Start));
            startServiceThread.Start();
        }

        protected override void OnStop()
        {
            System.Threading.Thread stopServiceThread = new System.Threading.Thread(new System.Threading.ThreadStart(applicationObj.Stop));
            stopServiceThread.Start();
        }
    }
}
