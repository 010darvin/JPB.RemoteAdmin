using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JPB.Communication;
using JPB.Communication;
using JPB.Communication.ComBase;
using JPB.Communication.ComBase.Messages;
using JPB.Communication.ComBase.Messages.Wrapper;
using JPB.Communication.ComBase.TCP;
using JPB.Communication.NativeWin.Serilizer;
using JPB.Communication.NativeWin.WinRT;
using JPB.RemoteAdmin.Common.Messages;

namespace JPB.RemoteAdmin.Client.Native
{
    public delegate void OnLoginReceived(NativeClient a, string c);

    public class NativeClient
    {
        public NativeClient()
        {
#if !DEBUG
            NetworkInfoBase.IpAddressExternal = new DnsFactory().GetHostAddresses("remote.no-ip.info").FirstOrDefault();
#endif
            if (!NetworkFactory.Created)
            {
                BinaryCompressedMessageSerializer.DefaultMessageSerlilizer.IlMergeSupport = true;
                BinaryCompressedMessageSerializer.DefaultMessageSerlilizer.PrevendDiscPageing = true;
                Networkbase.DefaultMessageSerializer = new BinaryCompressedMessageSerializer() { IlMergeSupport = true };
                NetworkFactory.Create(new WinRTFactory());
            }

            SharedConnections = new List<NativeClientInstance>();

            NetworkFactory.Instance.InitCommonSenderAndReciver(1337, 0);
            FileReceiver = NetworkFactory.Instance.GetReceiver(1338);
            FileReceiver.LargeMessageSupport = true;

            ImageReceiver = NetworkFactory.Instance.GetReceiver(1339); 
            ImageReceiver.Serlilizer = new DefaultMessageSerlilizer()
            {
                IlMergeSupport = true,
                PrevendDiscPageing = true
            };
            ImageReceiver.RegisterMessageBaseInbound(OnImageInbound, InfoState.OnScreenCaptureResult);

            NetworkFactory.Instance.Reciever.SharedConnection = true;
            NetworkFactory.Instance.Reciever.RegisterMessageBaseInbound(OnRegisterMessage, InfoState.OnIpChanged);

            ConnectionPool.Instance.OnConnectionCreated += Instance_OnConnectionCreated;
            ConnectionPool.Instance.OnConnectionClosed += Instance_OnConnectionClosed;
        }

        private void OnImageInbound(MessageBase obj)
        {
            var fod = SharedConnections.FirstOrDefault(s => s.HostAddress.Ip == obj.Sender);
            if (fod != null)
            {
                fod.RaiseOnImage(obj.Message as byte[]);
            }
        }

        public NativeClientInstance CreateInstance(ConnectionWrapper wrapper, IdHolder holder)
        {
            var fod = new NativeClientInstance(wrapper, holder);
            SharedConnections.Add(fod);
            return fod;
        }

        private static void OnFileInbound(LargeMessage obj)
        {
            var sender = SharedConnections.FirstOrDefault(s => s.HostAddress.Ip == obj.MetaData.Sender);
            if (sender != null)
                sender.OnFileInbound(obj);
        }

        public static TCPNetworkReceiver FileReceiver { get; set; }
        public static TCPNetworkReceiver ImageReceiver { get; set; }

        private static List<NativeClientInstance> SharedConnections;
        public event EventHandler<ConnectionWrapper> OnConnectionOpen;
        public event EventHandler<ConnectionWrapper> OnConnectionClosed;
        public event EventHandler OnFileInboundHandler;
        public event OnLoginReceived OnRegisterLoginMessage;

        protected virtual void RaiseRegisterLoginMessage(string ip)
        {
            var handler = OnRegisterLoginMessage;
            if (handler != null)
                handler(this, ip);
        }

        protected virtual void RaiseFileInbound()
        {
            var handler = OnFileInboundHandler;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        protected virtual void RaiseConnectionClosed(ConnectionWrapper e)
        {
            var handler = OnConnectionClosed;
            if (handler != null)
                handler(this, e);
        }

        protected virtual void RaiseConnectionOpen(ConnectionWrapper e)
        {
            var handler = OnConnectionOpen;
            if (handler != null)
                handler(this, e);
        }

        private void OnRegisterMessage(MessageBase obj)
        {
            RaiseRegisterLoginMessage(obj.Sender);

            if (SharedConnections.Any(s => s.HostAddress.Ip == obj.Sender))
            {
                return;
            }
            var fod = ConnectionPool.Instance.GetConnections().FirstOrDefault(s => s.Ip == obj.Sender);
            if (fod != null)
            {


                CreateInstance(
                    fod, IdManager.Instance.AddOrGet(obj.Message as string, obj.Sender, fod.TCPNetworkReceiver.Port));
            }
        }

        void Instance_OnConnectionCreated(object sender, ConnectionWrapper e)
        {
#if !DEBUG
            e.TCPNetworkSender.UseExternalIpAsSender = true;
#endif
            e.TCPNetworkReceiver.RegisterMessageBaseInbound(OnRegisterMessage, InfoState.OnIpChanged);
            RaiseConnectionOpen(e);
        }

        void Instance_OnConnectionClosed(object sender, ConnectionWrapper e)
        {
            SharedConnections.RemoveAll(s => s.HostAddress.Ip == e.Ip && s.HostAddress.TCPNetworkSender.Port == e.TCPNetworkSender.Port);
            RaiseConnectionClosed(e);
        }

        public ConnectionWrapper GetConnectionForIp(string ip)
        {
            var nativeClientInstance = SharedConnections.FirstOrDefault(s => s.HostAddress.Ip == ip);
            if (nativeClientInstance != null)
                return nativeClientInstance.HostAddress;
            return null;
        }
    }
}
