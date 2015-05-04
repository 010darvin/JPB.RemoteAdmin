using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JPB.RemoteAdmin.Server.Service.ServerAdapter;

namespace JPB.RemoteAdmin.Server.Service
{
    public partial class WinLog : ServiceBase
    {
        public WinLog()
        {
            InitializeComponent();
        }

        public Thread Thread { get; set; }

        protected override void OnStart(string[] args)
        {
            var nativeServerToWinServiceAdapter = new NativeServerToWinServiceAdapter();
            Thread = nativeServerToWinServiceAdapter.Thread;
        }

        protected override void OnStop()
        {
        }
    }
}
