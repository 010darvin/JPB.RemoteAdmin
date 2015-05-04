using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JPB.Communication.Contracts.Intigration;
using JPB.WPFBase.MVVM.ViewModel;

namespace JPB.RemoteAdmin.Client.WpfUi.ViewModel.ResolveIpWindow
{
    public class ResolveIpWindowViewModel : AsyncViewModelBase
    {
        public ResolveIpWindowViewModel()
        {
            
        }

        private IEnumerable<IPAddress> _ipAddresses;

        public IEnumerable<IPAddress> IpAddresses
        {
            get { return _ipAddresses; }
            set
            {
                _ipAddresses = value;
                SendPropertyChanged(() => IpAddresses);
            }
        }

        private IPAddress _selectedAddress;

        public IPAddress SelectedAddress
        {
            get { return _selectedAddress; }
            set
            {
                _selectedAddress = value;
                SendPropertyChanged(() => SelectedAddress);
            }
        }
    }
}
