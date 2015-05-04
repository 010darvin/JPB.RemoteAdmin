using System.IO;
using System.Linq;
using System.Windows;
using JPB.WPFBase.MVVM.DelegateCommand;
using JPB.WPFBase.MVVM.ViewModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using MessageBox = System.Windows.MessageBox;

namespace JPB.RemoteAdmin.Client.WpfUi.ViewModel.MainWindow
{
    public class DynCodeExecuterViewModel : AsyncViewModelBase
    {
        private readonly SingelClientViewModel _singelClientViewModel;

        public DynCodeExecuterViewModel(SingelClientViewModel singelClientViewModel)
        {
            _singelClientViewModel = singelClientViewModel;
            ExecuteCodeRemoteCommand = new DelegateCommand(ExecuteExecuteCodeRemote, CanExecuteExecuteCodeRemote);
            ResultSets = new ThreadSaveObservableCollection<string>();
        }

        private ThreadSaveObservableCollection<string> _resultSets;

        public ThreadSaveObservableCollection<string> ResultSets
        {
            get { return _resultSets; }
            set
            {
                _resultSets = value;
                SendPropertyChanged(() => ResultSets);
            }
        }

        public DelegateCommand ExecuteCodeRemoteCommand { get; private set; }

        public void ExecuteExecuteCodeRemote(object sender)
        {
            var diag = new CommonOpenFileDialog();

            var dialogResult = diag.ShowDialog();

            if(dialogResult == CommonFileDialogResult.Ok)
                return;

            var waitForExec = MessageBox.Show("Wait for execution?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
            base.SimpleWork(async () =>
            {
                var executeCodeRemote = await _singelClientViewModel.ClientInstance.ExecuteCodeRemote(diag.FileNames.ToArray(), waitForExec);
                ResultSets.Add(executeCodeRemote);
            });
        }

        public bool CanExecuteExecuteCodeRemote(object sender)
        {
            return IsNotWorking;
        }
    }
}
