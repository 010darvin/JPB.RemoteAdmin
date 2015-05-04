using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using JPB.Communication.ComBase;
using JPB.Communication.ComBase.Messages;
using JPB.Communication.ComBase.TCP;
using JPB.Communication.Contracts.Intigration;
using JPB.RemoteAdmin.Client.Native;
using JPB.RemoteAdmin.Client.WpfUi.ViewModel.ResolveIpWindow;
using JPB.RemoteAdmin.Common.Messages;
using JPB.WPFBase.MVVM.DelegateCommand;
using JPB.WPFBase.MVVM.ViewModel;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace JPB.RemoteAdmin.Client.WpfUi.ViewModel.MainWindow
{
    public class MainWindowViewModel : AsyncViewModelBase
    {
        public static string RootDirectory { get; set; }

        public NativeClient NativeClient { get; set; }

        public MainWindowViewModel()
        {
            RootDirectory = "C:\\";
            Clients = new ThreadSaveObservableCollection<SingelClientViewModel>();

            OpenDebugWindowCommand = new DelegateCommand(ExecuteOpenDebugWindow, CanExecuteOpenDebugWindow);
            StartDebugServerCommand = new DelegateCommand(ExecuteStartDebugServer, CanExecuteStartDebugServer);

            NetworkInfoBase.ResolveOwnIp += NetworkInfoBase_ResolveOwnIp;

            NativeClient = new NativeClient();
            NativeClient.OnConnectionClosed += NativeClient_OnConnectionClosed;
            NativeClient.OnRegisterLoginMessage += NativeClientOnOnRegisterLoginMessage;
            Application.Current.MainWindow.Closing += MainWindow_Closed;
        }

        IPAddress NetworkInfoBase_ResolveOwnIp(IPAddress[] arg)
        {
            IPAddress result = null;
            base.ThreadSaveAction(() =>
            {
                var vm = new ResolveIpWindowViewModel();
                vm.IpAddresses = arg;

                var resolveIpWindow = new Windows.ResolveIpWindow();
                resolveIpWindow.DataContext = vm;

                while (vm.SelectedAddress == null)
                {
                    try
                    {
                        resolveIpWindow.ShowDialog();
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
                result = vm.SelectedAddress;
            });
            return result;
        }

        private void NativeClientOnOnRegisterLoginMessage(NativeClient nativeClient, string ip)
        {
            var connection = ConnectionPool.Instance.GetConnections().FirstOrDefault(s => s.Ip == ip);
            if (connection != null)
            {
                lock (this)
                {

                    var exists = Clients.FirstOrDefault(s => s.ClientInstance.HostAddress.Ip == connection.Ip);
                    if (exists != null)
                        return;

                    //request the ID
                    var sendRequstMessage =
                        connection.TCPNetworkSender.SendRequstMessage<string>(
                            new RequstMessage()
                            {
                                InfoState = InfoState.RequstInitalId,
                                ExpectedResult = 1337
                            },
                            connection.Ip);
                    sendRequstMessage.Wait();

                    var pull = IdManager.Instance.AddOrGet(sendRequstMessage.Result, ip, connection.TCPNetworkReceiver.Port);
                    Clients.Add(new SingelClientViewModel(pull, NativeClient.CreateInstance(connection, pull)));
                }
            }
        }

        void NativeClient_OnConnectionClosed(object sender, ConnectionWrapper e)
        {
            var mainWindowViewModel = Clients.FirstOrDefault(s => s.TargetIP == e.Ip);
            if (mainWindowViewModel != null)
            {
                Clients.Remove(mainWindowViewModel);
            }
        }

        public DelegateCommand StartDebugServerCommand { get; private set; }

        public void ExecuteStartDebugServer(object sender)
        {
#if ExternalTest
            var path = @"Mircrosoft.Windows.Eventlogger.exe";

            if (File.Exists(path))
            {
               Process.Start(path);
            }
#else
            var path = @"Mircrosoft.Windows.Eventlogger.exe";

            if (File.Exists(path))
            {
                var debug = MessageBox.Show("Debug?", "Debug", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
                Process.Start(path, debug.ToString());
            }
            else
            {
                Process.Start(Assembly.GetEntryAssembly().Location, "--start");
            }
#endif
         
        }

        public bool CanExecuteStartDebugServer(object sender)
        {
            return true;
        }

        public DelegateCommand OpenDebugWindowCommand { get; private set; }

        public void ExecuteOpenDebugWindow(object sender)
        {
            //wind = new Window();
            //wind.Closing += wind_Closing;
            //wind.Content = new NetworkEventLogger();
            //wind.Show();

        }
        void wind_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            wind = null;
        }

        public bool CanExecuteOpenDebugWindow(object sender)
        {
            return wind == null;
        }

        Window wind;

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (wind != null)
                wind.Close();
        }

        private SingelClientViewModel _selectedClient;

        public SingelClientViewModel SelectedClient
        {
            get { return _selectedClient; }
            set
            {
                _selectedClient = value;
                SendPropertyChanged(() => SelectedClient);
            }
        }

        private ThreadSaveObservableCollection<SingelClientViewModel> _clients;

        public ThreadSaveObservableCollection<SingelClientViewModel> Clients
        {
            get { return _clients; }
            set
            {
                _clients = value;
                SendPropertyChanged(() => Clients);
            }
        }
    }
}
