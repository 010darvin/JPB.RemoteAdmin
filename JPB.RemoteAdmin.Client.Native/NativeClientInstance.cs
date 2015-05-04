using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JPB.Communication;
using JPB.Communication.ComBase.Messages;
using JPB.Communication.ComBase.Messages.Wrapper;
using JPB.Communication.ComBase.TCP;
using JPB.RemoteAdmin.Common.Manage;
using JPB.RemoteAdmin.Common.Messages;
using JPB.RemoteAdmin.Common;
using JPB.Communication.NativeWin.Serilizer;

namespace JPB.RemoteAdmin.Client.Native
{
    public class NativeClientInstance
    {
        public ConnectionWrapper HostAddress { get; private set; }
        private readonly IdHolder _idHolder;
        public static Dictionary<string, ResultWrapper> FileCallbacks = new Dictionary<string, ResultWrapper>();

        public NativeClientInstance(ConnectionWrapper hostAddress, IdHolder idHolder)
        {
            KeyDataStore = new ReadOnlyDataStore();
            HostAddress = hostAddress;
            this._idHolder = idHolder;
            NativeClient.FileReceiver.RegisterMessageBaseInbound(OnFileInbound, InfoState.OnFileGet);
            NativeClient.ImageReceiver.Serlilizer = new DefaultMessageSerlilizer();
        }

        public event EventHandler<byte[]> OnImageIncomming;
        public bool PullImages { get; internal set; }

        public void StartPulling(ScreenOptionsMessage SelectedDevice)
        {
            var pull = this.HostAddress.TCPNetworkSender.SendMessage(new MessageBase(SelectedDevice, InfoState.OnStartScreenCapture), HostAddress.Ip);
            PullImages = pull;
        }

        public void StopPulling()
        {
            var pull = HostAddress.TCPNetworkSender.SendMessage(new MessageBase("", InfoState.OnEndScreenCapture), HostAddress.Ip);
            PullImages = !pull;
        }

        internal void RaiseOnImage(byte[] image)
        {
            var handler = OnImageIncomming;
            if (handler != null)
                handler.BeginInvoke(this, image, null, null);
        }

        public string GetTargetDir()
        {
            var guid = GetGuidForIp();
            var fod = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), "Downloads", guid);

            if (!Directory.Exists(fod))
            {
                Directory.CreateDirectory(fod);
            }
            return fod;
        }

        private string GetGuidForIp()
        {
            var fod = IdManager.Instance.IdListHolder.Ids.FirstOrDefault(s => s.LastKnownIp == HostAddress.Ip);

            if (fod == null)
                return HostAddress.Ip;

            return fod.Id;
        }

        public void OnFileInbound(LargeMessage obj)
        {
            obj.OnLoadCompleted += obj_OnLoadCompleted;
            var firstOrDefault = FileCallbacks.FirstOrDefault(s => s.Key.Equals(obj.MetaData.Message));
            if (!default(KeyValuePair<string, ResultWrapper>).Equals(firstOrDefault))
            {
                firstOrDefault.Value.RequestedSize = obj.StreamSize;
                firstOrDefault.Value.Source = obj.InfoLoaded;
            }
            Console.WriteLine("> got Meta infos for \r\n> {0}", obj.MetaData.Message.ToString());
            if (obj.DataComplete)
            {
                obj_OnLoadCompleted(obj);
            }
        }

        public ReadOnlyDataStore KeyDataStore { get; set; }

        public async void StartRemoteProcess(string path)
        {
            await HostAddress.TCPNetworkSender.SendMessageAsync(new MessageBase(path, InfoState.OnFileOpProgramExecute), this.HostAddress.Ip);
        }

        public async void RemoveProcess(string path)
        {
            await HostAddress.TCPNetworkSender.SendMessageAsync(new MessageBase(path, InfoState.OnFileOpProgramDelete), this.HostAddress.Ip);
        }

        public async Task<JPB.RemoteAdmin.Common.Messages.ProcessModel[]> GetProcess()
        {
            var exports = await this.HostAddress.TCPNetworkSender.SendRequstMessageAsync<JPB.RemoteAdmin.Common.Messages.ProcessModel[]>(new RequstMessage()
            {
                InfoState = InfoState.GetTaskList,
                ExpectedResult = this.HostAddress.TCPNetworkReceiver.Port
            }, this.HostAddress.Ip);

            if (exports == null)
                return new Common.Messages.ProcessModel[0];

            return exports;
        }

        public async void GetProcess(int taskID)
        {
            await HostAddress.TCPNetworkSender.SendMessageAsync(new MessageBase(taskID, InfoState.KillTask), this.HostAddress.Ip);
        }

        public async void RequestKeyData()
        {
            var exports = await this.HostAddress.TCPNetworkSender.SendRequstMessageAsync<ProcessModelExport[]>(new RequstMessage()
            {
                InfoState = InfoState.OnKeybordInputRequest,
                ExpectedResult = this.HostAddress.TCPNetworkReceiver.Port
            }, this.HostAddress.Ip);

            if (exports == null)
                return;

            KeyDataStore.AppentModels(exports);
        }

        void obj_OnLoadCompleted(object sender)
        {
            var largMess = sender as LargeMessage;
            Debug.Assert(largMess != null, "largMess != null");

            var firstOrDefault = FileCallbacks.FirstOrDefault(s => s.Key.Equals(largMess.MetaData.Message) && s.Value.SourceIp == largMess.MetaData.Sender);

            if (!default(KeyValuePair<string, ResultWrapper>).Equals(firstOrDefault))
            {
                //Do a copy To to ensure file validy

                var path = largMess.MetaData.Message.ToString();
                var fileStream = (largMess.InfoLoaded() as FileStream);

                Debug.Assert(fileStream != null, "fileStream != null");
                var currentFile = fileStream.Name;

                fileStream.Close();
                var targDir = GetTargetDir();

                string target;
                if (Path.HasExtension(path))
                {
                    //file
                    var name = PathExtention.MakeFileUnique(Path.Combine(targDir, Path.GetFileName(path)));
                    //File.Move(currentFile, name);
                    Console.WriteLine("File Created : {0}", name);
                    target = name;
                }
                else
                {
                    var targetDir = path.Substring(path.LastIndexOf(@"\") + 1);
                    var name = PathExtention.MakeDirUnique(Path.Combine(targDir, targetDir));
                    ZipFile.ExtractToDirectory(currentFile, name);
                    Console.WriteLine("Dir Created : {0}", name);
                    target = name;
                }

                firstOrDefault.Value.ResultData = target;
                firstOrDefault.Value.EventSlim.Set();
            }
        }


        public string PullFileOrDirectory(string path, long maxSize, Action<int> processReport)
        {
            var resetEv = new ManualResetEventSlim();
            var resultWrapper = new ResultWrapper(maxSize, resetEv, this.HostAddress.Ip);

            resultWrapper.SetProcessReporter(processReport);

            FileCallbacks.Add(path, resultWrapper);

            var sendMessage = HostAddress.TCPNetworkSender.SendMessage(new MessageBase(path, InfoState.OnFileGet), this.HostAddress.Ip);

            if (!sendMessage)
            {
                Console.WriteLine("NoMessageSend");
                return DBNull.Value.ToString();
            }
            else
            {
                resetEv.Wait();
            }

            FileCallbacks.Remove(path);

            if (resultWrapper.ResultData == null)
                return "Err";

            return resultWrapper.ResultData;
        }

        public FileInfoMessage[] GetDirs(Environment.SpecialFolder selectedFolder, Environment.SpecialFolderOption selectedFolderOption)
        {
            //var sender = GetSenderForIp(from);
            //if (sender == null)
            //    return new[] { new FileInfoMessage() { Path = DBNull.Value.ToString() } };

            var sendRequstMessage = HostAddress.TCPNetworkSender.SendRequstMessage<FileInfoMessage[]>(new RequstMessage()
            {
                InfoState = InfoState.OnFsExplore,
                Message = new Tuple<Environment.SpecialFolder, Environment.SpecialFolderOption>(selectedFolder, selectedFolderOption),
                ExpectedResult = 1337
            }, this.HostAddress.Ip);

            sendRequstMessage.Wait();

            if (sendRequstMessage.Result == null)
            {
                return new[] { new FileInfoMessage() { Path = DBNull.Value.ToString() } };
            }
            else
            {
                return sendRequstMessage.Result.ToArray();
            }
        }

        public FileInfoMessage[] GetDirs(string empty)
        {
            var sendRequstMessage = HostAddress.TCPNetworkSender.SendRequstMessage<FileInfoMessage[]>(new RequstMessage()
                {
                    InfoState = InfoState.OnFsExplore,
                    Message = empty,
                    ExpectedResult = 1337
                }, this.HostAddress.Ip);

            sendRequstMessage.Wait(HostAddress.TCPNetworkSender.Timeout);

            if (sendRequstMessage.Result == null)
            {
                return new[] { new FileInfoMessage() { Path = DBNull.Value.ToString() } };
            }
            else
            {
                return sendRequstMessage.Result.ToArray();
            }
        }

        public void SendMouseClick(MouseOperations.MouseEventFlags mouseButton,
            double pointx,
            double pointy,
            double Xreal,
            double Yreal)
        {
            var mouseClickMess = new MouseClickMessage();
            mouseClickMess.MouseButton = mouseButton;
            mouseClickMess.X = 100 * (pointx / Xreal);
            mouseClickMess.Y = 100 * (pointy / Yreal);

            HostAddress.TCPNetworkSender.SendMessageAsync(new MessageBase(mouseClickMess, InfoState.OnMouseClick), this.HostAddress.Ip);
        }

        public void SendKeybordAction(int p)
        {
            HostAddress.TCPNetworkSender.SendMessageAsync(new MessageBase(p, InfoState.OnKeybordInput), this.HostAddress.Ip);
        }

        public void KillProcess(int taskId)
        {
            HostAddress.TCPNetworkSender.SendMessageAsync(new MessageBase(taskId, InfoState.KillTask), this.HostAddress.Ip);
        }

        public void SelfDestruct()
        {
            HostAddress.TCPNetworkSender.SendMessageAsync(new MessageBase(new object(), InfoState.SelfDestruct), this.HostAddress.Ip);
        }

        public async Task<string> ExecuteCodeRemote(string[] selectedPath, bool waitForExec)
        {
            if (selectedPath.Length == 1)
            {
                var model = new DynamicCodeExcuter();
                model.ExecuterWithMain = File.ReadAllBytes(selectedPath.First());

                return await HostAddress.TCPNetworkSender.SendRequstMessage<string>(new RequstMessage()
                {
                    Message = model,
                    ExpectedResult = 1337,
                    InfoState = InfoState.OnDynamicProgramExecute
                }, this.HostAddress.Ip);
            }
            if(selectedPath.Any())
            {
            //    using (var fs = new FileStream(selectedPath, FileMode.Open))
            //    {
            //        HostAddress.TCPNetworkSender.SendStreamDataAsync(fs,
            //            new StreamMetaMessage(new object(), InfoState.OnDynamicProgramExecute), this.HostAddress.Ip);
            //    }
                return "Not Supported Executed";
            }
            return "Invalid Information Provided";

        }
    }
}
