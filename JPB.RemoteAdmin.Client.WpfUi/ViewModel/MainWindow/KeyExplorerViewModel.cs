using JPB.RemoteAdmin.Common.Manage;
using JPB.WPFBase.MVVM.DelegateCommand;
using JPB.WPFBase.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPB.RemoteAdmin.Client.WpfUi.ViewModel.MainWindow
{
    public class KeyExplorerViewModel : AsyncViewModelBase
    {
        SingelClientViewModel _singelClientViewModel;
        public KeyExplorerViewModel(SingelClientViewModel singelClientViewModel)
        {
            KeyExplorerViews = new ThreadSaveObservableCollection<ProcessModelExport>();
            _singelClientViewModel = singelClientViewModel;
            GetCurrentKeyStrokesCommand = new DelegateCommand(GetKeyStrokeFromServer, CanGetKeyStrokeFromServer);
        }
        
        public ThreadSaveObservableCollection<ProcessModelExport> KeyExplorerViews { get; set; }

        public DelegateCommand GetCurrentKeyStrokesCommand { get; set; }
 
        private void GetKeyStrokeFromServer(object sender)
        {
            base.SimpleWork(() =>
            {
                _singelClientViewModel.ClientInstance.RequestKeyData();
                var dataStore = _singelClientViewModel.ClientInstance.KeyDataStore;

                KeyExplorerViews.Clear();

                foreach (var item in dataStore.ProcessModels)
                {
                    KeyExplorerViews.Add(item);
                }
            });
        }

        private bool CanGetKeyStrokeFromServer(object obj)
        {
            return CheckCanExecuteCondition();
        }
    }
}
