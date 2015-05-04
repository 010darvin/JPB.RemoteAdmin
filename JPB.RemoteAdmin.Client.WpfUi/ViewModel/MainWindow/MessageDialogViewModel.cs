using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JPB.Communication.ComBase.Messages;
using JPB.RemoteAdmin.Common.Contracts;
using JPB.RemoteAdmin.Common.Messages;
using JPB.WPFBase.MVVM.DelegateCommand;
using JPB.WPFBase.MVVM.ViewModel;

namespace JPB.RemoteAdmin.Client.WpfUi.ViewModel.MainWindow
{
    public class MessageDialogViewModel : AsyncViewModelBase
    {
        private readonly SingelClientViewModel _singelClientViewModel;

        public MessageDialogViewModel(SingelClientViewModel singelClientViewModel)
        {
            _singelClientViewModel = singelClientViewModel;
            RunSelectedContractOnClientCommand = new DelegateCommand(ExecuteRunSelectedContractOnClient, CanExecuteRunSelectedContractOnClient);
            MessageContractsBase = MessageBoxContractManager.GetContracts().Select(s => new MessageBoxContractViewModel(s)).ToArray();
        }

        public MessageBoxContractViewModel[] MessageContractsBase { get; private set; }

        private MessageBoxContractViewModel _selectedContractBase;

        public MessageBoxContractViewModel SelectedContractBase
        {
            get { return _selectedContractBase; }
            set
            {
                _selectedContractBase = value;
                SendPropertyChanged(() => SelectedContractBase);
            }
        }

        private object _receivedResult;

        public object ReceivedResult
        {
            get { return _receivedResult; }
            set
            {
                _receivedResult = value;
                base.ThreadSaveAction(() => SendPropertyChanged(() => ReceivedResult));
            }
        }

        public DelegateCommand RunSelectedContractOnClientCommand { get; private set; }

        public void ExecuteRunSelectedContractOnClient(object sender)
        {
            base.SimpleWork(async () =>
            {
                foreach (var propertyInfo in this.SelectedContractBase.MessageBoxContractBase.GetType().GetProperties())
                {
                    var fod = this.SelectedContractBase.Params.FirstOrDefault(s => s.Name == propertyInfo.Name);
                    if (fod != null)
                    {
                        propertyInfo.SetValue(this.SelectedContractBase.MessageBoxContractBase, fod.Value);
                    }
                }

                ReceivedResult =
                await
                    _singelClientViewModel.ClientInstance.HostAddress.TCPNetworkSender.SendRequstMessage<object>(
                        new RequstMessage()
                        {
                            InfoState = InfoState.OnMessageBoxRequest,
                            Message = this.SelectedContractBase.MessageBoxContractBase,
                            ExpectedResult = 1337
                        }, _singelClientViewModel.TargetIP);
            });
        }

        public bool CanExecuteRunSelectedContractOnClient(object sender)
        {
            return base.CheckCanExecuteCondition() && SelectedContractBase != null;
        }
    }

    public class MessageBoxContractViewModel : ViewModelBase
    {
        public MessageBoxContractBase MessageBoxContractBase { get; private set; }

        public MessageBoxContractViewModel(MessageBoxContractBase messageBoxContractBase)
        {
            MessageBoxContractBase = messageBoxContractBase;
            Params = MessageBoxContractBase.Params.Select(s => new MessageBoxParameterViewModel(s)).ToList();
            SendPropertyChanged(() => Params);
        }

        public string Id
        {
            get { return MessageBoxContractBase.Id; }
        }

        public List<MessageBoxParameterViewModel> Params { get; set; }
    }
}
