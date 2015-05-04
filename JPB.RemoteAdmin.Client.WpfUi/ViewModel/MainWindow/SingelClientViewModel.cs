using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using JPB.Communication;
using JPB.RemoteAdmin.Client.Native;
using JPB.WPFBase.MVVM.ViewModel;

namespace JPB.RemoteAdmin.Client.WpfUi.ViewModel.MainWindow
{
    public class SingelClientViewModel : AsyncViewModelBase
    {
        public IdHolder Holder { get; set; }
        public NativeClientInstance ClientInstance { get; private set; }

        public SingelClientViewModel(IdHolder holder, NativeClientInstance hostAddress)
        {
            TargetIP = holder.LastKnownIp;
            Holder = holder;
            this.ClientInstance = hostAddress;
            FileSystemViewModel = new FileSystemViewModel(this);
            DynCodeExecuterViewModel = new DynCodeExecuterViewModel(this);
            ScreenCaptureViewModel = new ScreenCaptureViewModel(this);
            SystemStateViewModel = new SystemStateViewModel(this);
            KeyExplorereViewModel = new KeyExplorerViewModel(this);
            TaskExplorerViewModel = new ProcessExplorerViewModel(this);
            MessageDialogViewModel = new MessageDialogViewModel(this);
        }
        
        private string _targetIP;

        public string TargetIP
        {
            get { return _targetIP; }
            set
            {
                _targetIP = value;
                SendPropertyChanged(() => TargetIP);
            }
        }

        public MessageDialogViewModel MessageDialogViewModel { get; set; }

        public ProcessExplorerViewModel TaskExplorerViewModel { get; set; }

        public KeyExplorerViewModel KeyExplorereViewModel { get; set; }

        private FileSystemViewModel _fileSystemViewModel;

        public FileSystemViewModel FileSystemViewModel
        {
            get { return _fileSystemViewModel; }
            set
            {
                _fileSystemViewModel = value;
                SendPropertyChanged(() => FileSystemViewModel);
            }
        }

        private DynCodeExecuterViewModel _dynCodeExecuterViewModel;

        public DynCodeExecuterViewModel DynCodeExecuterViewModel
        {
            get { return _dynCodeExecuterViewModel; }
            set
            {
                _dynCodeExecuterViewModel = value;
                SendPropertyChanged(() => DynCodeExecuterViewModel);
            }
        }

        private SystemStateViewModel _systemStateViewModel;

        public SystemStateViewModel SystemStateViewModel
        {
            get { return _systemStateViewModel; }
            set
            {
                _systemStateViewModel = value;
                SendPropertyChanged(() => SystemStateViewModel);
            }
        }

        private ScreenCaptureViewModel _screenCaptureViewModel;

        public ScreenCaptureViewModel ScreenCaptureViewModel
        {
            get { return _screenCaptureViewModel; }
            set
            {
                _screenCaptureViewModel = value;
                SendPropertyChanged(() => ScreenCaptureViewModel);
            }
        }
    }
}
