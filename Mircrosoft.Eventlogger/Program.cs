using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using JPB.Communication.ComBase;
using JPB.RemoteAdmin.Server.Native;
using IPAddress = JPB.Communication.Contracts.Intigration.IPAddress;

namespace Mircrosoft.Windows.Eventlogger
{
    public class Program
    {
        public NativeServer nativeServer;

        [STAThread]
        static void Main(string[] args)
        {
#pragma warning disable 4014
            new Program().GoGoGatdet();
#pragma warning restore 4014
            var debuggerWindow = new Application();
            debuggerWindow.Run();
        }

        public async Task GoGoGatdet()
        {
            NetworkInfoBase.ResolveOwnIp += NetworkInfoBase_ResolveOwnIp;
            NetworkInfoBase.ResolveDistantIp += NetworkInfoBase_ResolveOwnIp;

            nativeServer = new NativeServer
            {
                AddKeyListener = false,
                AutoAttachHandler = true,
                CallbackOnAppError = true
            };

            nativeServer.Init(1337, 1338);
            await nativeServer.Start();
        }

        private IPAddress NetworkInfoBase_ResolveOwnIp(IPAddress[] arg1, string arg2)
        {
            return arg1.FirstOrDefault(s => new System.Net.IPAddress(s.Address).AddressFamily == AddressFamily.InterNetwork);
        }

        IPAddress NetworkInfoBase_ResolveOwnIp(IPAddress[] arg)
        {
            return arg.FirstOrDefault(s => new System.Net.IPAddress(s.Address).AddressFamily == AddressFamily.InterNetwork);

        }
    }
}
