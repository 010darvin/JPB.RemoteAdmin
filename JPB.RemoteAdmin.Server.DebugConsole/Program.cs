using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using JPB.Communication;
using JPB.Communication.ComBase;
using JPB.Communication.ComBase.Messages;
using JPB.Communication.Contracts.Intigration;
using JPB.RemoteAdmin.Server.Native;

namespace JPB.RemoteAdmin.Server.DebugConsole
{
    public class Program
    {
        private static bool _shouldReconnect;

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
            Console.WriteLine("Open");
            NetworkInfoBase.ResolveOwnIp += NetworkInfoBase_ResolveOwnIp;
            NetworkInfoBase.ResolveDistantIp += NetworkInfoBase_ResolveOwnIp;
            var nativeServer = new NativeServer();

            nativeServer.AddKeyListener = false;
            nativeServer.AutoAttachHandler = true;
            nativeServer.CallbackOnAppError = true;
            Console.WriteLine("Init");
            nativeServer.Init(1337, 1338);
            nativeServer.OnStartLoopFeedback += nativeServer_OnStartLoopFeedback;
            await nativeServer.Start();
        }

        void nativeServer_OnStartLoopFeedback(object sender, string e)
        {
            Console.WriteLine(e);
        }

        private IPAddress NetworkInfoBase_ResolveOwnIp(IPAddress[] arg1, string arg2)
        {
            Console.WriteLine("Found multi addresses Send Msg");
            var fod = arg1.FirstOrDefault(s => new System.Net.IPAddress(s.Address).AddressFamily == AddressFamily.InterNetwork);
            Console.WriteLine("Will select {0}", fod);
            return fod;
        }

        IPAddress NetworkInfoBase_ResolveOwnIp(IPAddress[] arg)
        {
            Console.WriteLine("Found multi addresses");
            foreach (var ipAddress in arg)
            {
                Console.WriteLine(ipAddress.ToString());
            }

            var fod = arg.FirstOrDefault(s => new System.Net.IPAddress(s.Address).AddressFamily == AddressFamily.InterNetwork);
            Console.WriteLine("Will select {0}", fod);
            return fod;
        }
    }
}
