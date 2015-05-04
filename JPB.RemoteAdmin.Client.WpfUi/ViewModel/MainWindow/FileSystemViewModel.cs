using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JPB.Communication;
using JPB.Communication.ComBase.Messages;
using JPB.RemoteAdmin.Common.Messages;
using JPB.WPFBase.MVVM.DelegateCommand;
using JPB.WPFBase.MVVM.ViewModel;

namespace JPB.RemoteAdmin.Client.WpfUi.ViewModel.MainWindow
{
    public class FileSystemViewModel : AsyncViewModelBase
    {
        private readonly SingelClientViewModel _singelClientViewModel;

        public FileSystemViewModel(SingelClientViewModel singelClientViewModel)
        {
            _singelClientViewModel = singelClientViewModel;
            NetworkFactory.Instance.Reciever.RegisterMessageBaseInbound(OnRegisterMessage, InfoState.OnIpChanged);
            Root = new ThreadSaveObservableCollection<FileInfoViewModel>();
            OpenFileCommand = new DelegateCommand(ExecuteOpenFile, CanExecuteOpenFile);
            LoadFolderCommand = new DelegateCommand(ExecuteLoadFolder, CanExecuteLoadFolder);
            ReloadCommand = new DelegateCommand(ExecuteReload, CanExecuteReload);
            ExecuteSearchCommand = new AsyncDelegateCommand(ExecuteExecuteSearch, CanExecuteExecuteSearch);
            ExecuteGoToCommand = new AsyncDelegateCommand(ExecuteExecuteGoTo);
            BackCommand = new DelegateCommand(ExecuteBack, CanExecuteBack);
            _prev = new Stack<FileInfoViewModel>();
            FolderShortcuts = Enum.GetValues(typeof(Environment.SpecialFolder)) as Environment.SpecialFolder[];
            Options = Enum.GetValues(typeof(Environment.SpecialFolderOption)) as Environment.SpecialFolderOption[];
        }

        private Environment.SpecialFolderOption[] _options;

        public Environment.SpecialFolderOption[] Options
        {
            get { return _options; }
            set
            {
                _options = value;
                SendPropertyChanged(() => Options);
            }
        }

        private Environment.SpecialFolder[] _folderShortcuts;

        public Environment.SpecialFolder[] FolderShortcuts
        {
            get { return _folderShortcuts; }
            set
            {
                _folderShortcuts = value;
                SendPropertyChanged(() => FolderShortcuts);
            }
        }

        private Environment.SpecialFolderOption _selectedFolderOption;

        public Environment.SpecialFolderOption SelectedFolderOption
        {
            get { return _selectedFolderOption; }
            set
            {
                _selectedFolderOption = value;
                SendPropertyChanged(() => SelectedFolderOption);
            }
        }

        private Environment.SpecialFolder _selectedFolder;

        public Environment.SpecialFolder SelectedFolder
        {
            get { return _selectedFolder; }
            set
            {
                _selectedFolder = value;
                SendPropertyChanged(() => SelectedFolder);
            }
        }

        public DelegateCommand ReloadCommand { get; private set; }

        public void ExecuteReload(object sender)
        {
            base.SimpleWork(() =>
            {
                Root.Clear();

                var fileInfoViewModels = _singelClientViewModel.ClientInstance.GetDirs(string.Empty).Select(s => new FileInfoViewModel(s, _singelClientViewModel));
                foreach (var fileInfoViewModel in fileInfoViewModels)
                {
                    Root.Add(fileInfoViewModel);
                }
            });
        }

        public bool CanExecuteReload(object sender)
        {
            return IsNotWorking;
        }

        private void OnRegisterMessage(MessageBase obj)
        {
            if (obj.Sender == _singelClientViewModel.TargetIP && Root.All(s => s.Self.Path == DBNull.Value.ToString()) && IsNotWorking)
                ExecuteReload(null);
        }

        public override bool OnTaskException(Exception exception)
        {
            return base.OnTaskException(exception);
        }

        private int _processOfLoading;

        public int ProcessOfLoading
        {
            get { return _processOfLoading; }
            set
            {
                _processOfLoading = value;
                base.ThreadSaveAction(() =>
                {
                    SendPropertyChanged(() => ProcessOfLoading);
                });
            }
        }

        private string _searchString;

        public string SearchString
        {
            get { return _searchString; }
            set
            {
                _searchString = value;
                SendPropertyChanged(() => SearchString);
            }
        }

        public AsyncDelegateCommand ExecuteGoToCommand { get; private set; }

        public void ExecuteExecuteGoTo(object sender)
        {
            var fileInfoViewModels = _singelClientViewModel.ClientInstance.GetDirs(SelectedFolder, SelectedFolderOption).Select(s => new FileInfoViewModel(s, _singelClientViewModel));
            this.CurrentRoot = new FileInfoViewModel(fileInfoViewModels);
        }

        public AsyncDelegateCommand ExecuteSearchCommand { get; private set; }

        public async void ExecuteExecuteSearch(object sender)
        {
            var sendRequstMessage = await this._singelClientViewModel.ClientInstance.HostAddress.TCPNetworkSender.SendRequstMessage<FileInfoMessage[]>(new RequstMessage()
            {
                Message = SearchString,
                InfoState = InfoState.SearchRequest,
                ExpectedResult = 1337
            }, this._singelClientViewModel.TargetIP);

            if (sendRequstMessage != null && sendRequstMessage.Any())
            {
                Root.Clear();
                foreach (var fileInfoViewModel in sendRequstMessage)
                {
                    Root.Add(new FileInfoViewModel(fileInfoViewModel, _singelClientViewModel));
                }
            }
        }

        private bool CanExecuteExecuteSearch(object obj)
        {
            return !string.IsNullOrEmpty(SearchString) && this._singelClientViewModel.ClientInstance.HostAddress.TCPNetworkSender != null;
        }

        public ThreadSaveObservableCollection<FileInfoViewModel> Root { get; set; }

        public DelegateCommand BackCommand { get; private set; }

        public void ExecuteBack(object sender)
        {
            _currentRoot = _prev.Pop();
            SendPropertyChanged(() => CurrentRoot);
        }

        public bool CanExecuteBack(object sender)
        {
            return _prev.Any();
        }

        private FileInfoViewModel _selectedSub;
        private FileInfoViewModel _currentRoot;
        private Stack<FileInfoViewModel> _prev;

        public FileInfoViewModel SelectedSub
        {
            get { return _selectedSub; }
            set
            {
                _selectedSub = value;
                SendPropertyChanged(() => SelectedSub);
            }
        }

        public FileInfoViewModel CurrentRoot
        {
            get { return _currentRoot; }
            set
            {
                if (value == null || (value.Self != null && value.Self.Type == FileType.File))
                    return;

                if (_currentRoot != null)
                    _prev.Push(_currentRoot);

                _currentRoot = value;
                SendPropertyChanged(() => CurrentRoot);
            }
        }

        private ThreadSaveObservableCollection<HistoryItem> _historyItems;

        public ThreadSaveObservableCollection<HistoryItem> HistoryItems
        {
            get { return _historyItems; }
            set
            {
                _historyItems = value;
                SendPropertyChanged(() => HistoryItems);
            }
        }

        public DelegateCommand OpenFileCommand { get; private set; }

        public void ExecuteOpenFile(object sender)
        {
            base.SimpleWork(() =>
            {
                try
                {
                    Process.Start(SelectedSub.PathToTarget);
                }
                catch (Exception)
                {
                    Process.Start("explorer.exe", string.Format("/Select {0}", SelectedSub.PathToTarget));
                }
            });

        }

        public bool CanExecuteOpenFile(object sender)
        {
            return SelectedSub != null && !string.IsNullOrEmpty(SelectedSub.PathToTarget) &&
                (File.Exists(SelectedSub.PathToTarget) || Directory.Exists(SelectedSub.PathToTarget));
        }

        public DelegateCommand LoadFolderCommand { get; private set; }

        public void ExecuteLoadFolder(object sender)
        {
            base.SimpleWork(() =>
            {
                var targ = SelectedSub;

                targ.PathToTarget = this._singelClientViewModel.ClientInstance.PullFileOrDirectory(targ.Self.Path, -1,
                    s =>
                    {
                        targ.ProcessOfLoading = s;
                    });
            });
        }

        public bool CanExecuteLoadFolder(object sender)
        {
            return SelectedSub != null 
                && SelectedSub.Self != null 
                && !Path.HasExtension(SelectedSub.Self.Path) && !IsWorking;
        }
    }

    public class HistoryItem
    {
        public HistoryType Type { get; set; }
    }

    public enum HistoryType
    {
        MoveDir,
        PullDir,
        PullFile,
    }

    public class FolderShortcut
    {
        public FolderShortcut(string descriptor, Environment.SpecialFolder path)
        {
            Path = path;
            Descriptor = descriptor;
        }

        public string Descriptor { get; private set; }
        public Environment.SpecialFolder Path { get; private set; }
    }
}
