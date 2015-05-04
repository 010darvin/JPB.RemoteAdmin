using JPB.Communication;
using JPB.Communication;
using JPB.Communication.ComBase.Messages;
using JPB.RemoteAdmin.Common.Messages;
using JPB.WPFBase.MVVM.DelegateCommand;
using JPB.WPFBase.MVVM.ViewModel;

namespace JPB.RemoteAdmin.Client.WpfUi.ViewModel.MainWindow
{
    public class SystemStateViewModel : AsyncViewModelBase
    {
        private readonly SingelClientViewModel _singelClientViewModel;

        public SystemStateViewModel(SingelClientViewModel singelClientViewModel)
        {
            _singelClientViewModel = singelClientViewModel;
            Exceptions = new ThreadSaveObservableCollection<string>();
            NetworkFactory.Instance.Reciever.RegisterMessageBaseInbound(OnRemoteException, InfoState.OnException);
            RestartCommand = new DelegateCommand(ExecuteRestart, CanExecuteRestart);
            ReconnectCommand = new DelegateCommand(ExecuteReconnect, CanExecuteReconnect);
        }

        private ThreadSaveObservableCollection<string> _exceptions;

        public ThreadSaveObservableCollection<string> Exceptions
        {
            get { return _exceptions; }
            set
            {
                _exceptions = value;
                SendPropertyChanged(() => Exceptions);
            }
        }

        private void OnRemoteException(MessageBase messageBase)
        {
            if (messageBase.Sender == this._singelClientViewModel.TargetIP)
                Exceptions.Add(messageBase.Message as string);
        }

        public DelegateCommand RestartCommand { get; private set; }

        public void ExecuteRestart(object sender)
        {
            _singelClientViewModel.ClientInstance.HostAddress.TCPNetworkSender.SendMessageAsync(
                new MessageBase(null, InfoState.Restart), _singelClientViewModel.TargetIP);
        }

        public bool CanExecuteRestart(object sender)
        {
            return true;
        }

        public DelegateCommand ReconnectCommand { get; private set; }

        public void ExecuteReconnect(object sender)
        {
            _singelClientViewModel.ClientInstance.HostAddress.TCPNetworkSender.SendMessageAsync(
              new MessageBase(null, InfoState.Reconnect), _singelClientViewModel.TargetIP);
        }

        public bool CanExecuteReconnect(object sender)
        {
            return true;
        }
    }
}
