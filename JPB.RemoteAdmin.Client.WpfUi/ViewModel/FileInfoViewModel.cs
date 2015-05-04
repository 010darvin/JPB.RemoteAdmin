using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JPB.RemoteAdmin.Client.Native;
using JPB.RemoteAdmin.Client.WpfUi.ViewModel.MainWindow;
using JPB.RemoteAdmin.Common.Messages;
using JPB.WPFBase.MVVM.DelegateCommand;
using JPB.WPFBase.MVVM.ViewModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace JPB.RemoteAdmin.Client.WpfUi.ViewModel
{
    public class FileInfoViewModel : AsyncViewModelBase
    {
        private readonly SingelClientViewModel _singelClientViewModel;

        public FileInfoViewModel(FileInfoMessage info, SingelClientViewModel singelClientViewModel)
            : this()
        {
            _singelClientViewModel = singelClientViewModel;
            Self = info;
            CheckForExistingContent();
        }

        public void CheckForExistingContent()
        {
            if (_singelClientViewModel == null)
                return;

            base.SimpleWork(() =>
            {
                var combine = Path.Combine(this._singelClientViewModel.ClientInstance.GetTargetDir(), Path.GetFileName(Self.Path));
                if (Self.Type == FileType.File)
                {
                    if (File.Exists(combine))
                    {
                        PathToTarget = combine;
                    }
                }
                else if(Self.Type == FileType.Directory)
                {
                    if (Directory.Exists(combine))
                    {
                        PathToTarget = combine;
                    }
                }
            });
        }

        public FileInfoViewModel()
        {
            FileInfoMessages = new ThreadSaveObservableCollection<FileInfoViewModel>();
            ExecuteProcessCommand = new DelegateCommand(ExecuteExecuteProcess, CanExecuteExecuteProcess);
            RemoveProcessCommand = new DelegateCommand(ExecuteRemoveProcessCommand);
            LoadCommand = new DelegateCommand(ExecuteLoad, CanExecuteLoad);
        }

        public FileInfoViewModel(IEnumerable<FileInfoViewModel> sub) 
            : this()
        {
            FileInfoMessages = new ThreadSaveObservableCollection<FileInfoViewModel>(sub);  
        }

        private string _pathToTarget;

        public string PathToTarget
        {
            get { return _pathToTarget; }
            set
            {
                _pathToTarget = value;
                SendPropertyChanged(() => PathToTarget);
            }
        }

        ImageSource _icon;

        public ImageSource Icon
        {
            get
            {
                if (_icon != null)
                    return _icon;

                using (var memstream = new MemoryStream(Self.Icon))
                {
                    var mp = new BitmapImage();
                    mp.BeginInit();
                    mp.StreamSource = memstream;
                    
                    mp.EndInit();
                    _icon = mp;
                }
                return _icon;
            }
        }

        public FileInfoMessage Self { get; set; }
        public ThreadSaveObservableCollection<FileInfoViewModel> FileInfoMessages { get; private set; }
        public DelegateCommand LoadCommand { get; private set; }

        public DelegateCommand RemoveProcessCommand { get; private set; }

        private async void ExecuteRemoveProcessCommand(object sender)
        {
            this._singelClientViewModel.ClientInstance.RemoveProcess(Self.Path);
        }

        public DelegateCommand ExecuteProcessCommand { get; private set; }

        public async void ExecuteExecuteProcess(object sender)
        {
            this._singelClientViewModel.ClientInstance.StartRemoteProcess(Self.Path);
        }

        public bool CanExecuteExecuteProcess(object sender)
        {
            return true;
        }

        private int _processOfLoading;

        public int ProcessOfLoading
        {
            get { return _processOfLoading; }
            set
            {
                _processOfLoading = value;
                ThreadSaveAction(() =>
                {
                    SendPropertyChanged(() => ProcessOfLoading);
                });
            }
        }

        private bool _isExpanded;

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value && !_isExpanded)
                {
                    if (LoadCommand.CanExecute(this))
                        LoadCommand.Execute(this);
                }
                _isExpanded = value;
                SendPropertyChanged(() => IsExpanded);
            }
        }

        private void ExecuteLoad(object sender)
        {
            FileInfoMessages.Clear();

            base.SimpleWork(() =>
            {
                if (Self.Type == FileType.File)
                {
                    PathToTarget = this._singelClientViewModel.ClientInstance.PullFileOrDirectory(Self.Path, Self.Size, s =>
                    {
                        ProcessOfLoading = s;
                    });
                }
                else
                {
                    var fileInfoMessages = this._singelClientViewModel.ClientInstance.GetDirs(Self.Path).Select(s => new FileInfoViewModel(s, _singelClientViewModel));
                    _singelClientViewModel.FileSystemViewModel.CurrentRoot = this;
                    base.ThreadSaveAction(() =>
                    {
                        foreach (var fileInfoViewModel in fileInfoMessages)
                        {
                            this.FileInfoMessages.Add(fileInfoViewModel);
                        }
                    });
                }
            });
        }

        public bool CanExecuteLoad(object sender)
        {
            return _singelClientViewModel != null && Self.Path != DBNull.Value.ToString() && !FileInfoMessages.Any();
        }
    }
}