using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JPB.RemoteAdmin.Server.Native;

namespace JPB.RemoteAdmin.Server.Service.ServerAdapter
{
    public class NativeServerToWinServiceAdapter
    {
        public NativeServerToWinServiceAdapter()
        {
            Thread = new Thread(ServerProcessor);
            Thread.Start();
        }

        bool _shouldReconnect;

        private async void ServerProcessor()
        {
            var nativeServer = new NativeServer();

            nativeServer.AddKeyListener = false;
            nativeServer.AutoAttachHandler = true;
            nativeServer.CallbackOnAppError = true;
            nativeServer.Init(1337, 1338);
            bool connected = false;

            while (true)
            {
                try
                {
                    if (_shouldReconnect || !nativeServer.CheckConnection())
                    {
                        _shouldReconnect = false;
                        connected = await nativeServer.Reconnect(nativeServer.Target);
                    }

                    if (connected)
                    {
                        connected = await nativeServer.SendAliveMessage();
                    }
                     var sleep = !connected ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(20);

                    Thread.Sleep(sleep);
                }
                catch (Exception e)
                {
                    _shouldReconnect = true;
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                }
                finally
                {
                    GC.Collect();
                    GC.WaitForFullGCComplete();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }
        }

        public Thread Thread;

        public NativeServer NativeServer { get; set; }
    }
}
