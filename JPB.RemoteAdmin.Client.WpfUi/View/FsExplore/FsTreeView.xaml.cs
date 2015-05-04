using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using JPB.RemoteAdmin.Client.WpfUi.ViewModel;
using JPB.RemoteAdmin.Client.WpfUi.ViewModel.MainWindow;

namespace JPB.RemoteAdmin.Client.WpfUi.View.FsExplore
{
    /// <summary>
    /// Interaction logic for FsTreeView.xaml
    /// </summary>
    public partial class FsTreeView : UserControl
    {
        public FsTreeView()
        {
            InitializeComponent();
        }

        private FileSystemViewModel _fileSystemViewModel;

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_fileSystemViewModel == null)
            {
                _fileSystemViewModel = DataContext as FileSystemViewModel;
            }

            if (_fileSystemViewModel != null)
            {
                _fileSystemViewModel.SelectedSub = e.NewValue as FileInfoViewModel;
                if (_fileSystemViewModel.SelectedSub != null)
                {
                    _fileSystemViewModel.ThreadSaveAction(() =>
                    {
                        if (PreviewerWebBrowser != null)
                            PreviewerWebBrowser.Navigate("about:blank");
                    });

                    _fileSystemViewModel.SelectedSub.PropertyChanged -= SelectedFileInfoViewModelOnPropertyChanged;
                    _fileSystemViewModel.SelectedSub.PropertyChanged += SelectedFileInfoViewModelOnPropertyChanged;

                    SelectedFileInfoViewModelOnPropertyChanged(_fileSystemViewModel.SelectedSub, null);
                }
            }
        }

        public static readonly DependencyProperty PreviewerWebBrowserProperty = DependencyProperty.Register(
            "PreviewerWebBrowser", typeof(WebBrowser), typeof(FsTreeView), new PropertyMetadata(default(WebBrowser)));

        public WebBrowser PreviewerWebBrowser
        {
            get { return (WebBrowser)GetValue(PreviewerWebBrowserProperty); }
            set { SetValue(PreviewerWebBrowserProperty, value); }
        }

        private void SelectedFileInfoViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var typ = sender as FileInfoViewModel;

            if (propertyChangedEventArgs == null || propertyChangedEventArgs.PropertyName == "PathToTarget")
                if (!string.IsNullOrEmpty(typ.PathToTarget) && System.IO.Path.HasExtension(typ.PathToTarget))
                {

                    _fileSystemViewModel.ThreadSaveAction(() =>
                    {
                        if (PreviewerWebBrowser != null)
                            PreviewerWebBrowser.Navigate(typ.PathToTarget);
                    });
                }
        }
    }
}
