using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace JPB.RemoteAdmin.Server.Service
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var startAsUser = false;

            var args = Environment.CommandLine;
            if (args.Any())
            {
                var first = args.First();

            }

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new WinLog() 
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
