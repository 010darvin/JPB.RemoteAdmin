using System.Windows;
using JPB.RemoteAdmin.Client.WpfUi.ViewModel.MainWindow;

namespace JPB.RemoteAdmin.Client.WpfUi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}
