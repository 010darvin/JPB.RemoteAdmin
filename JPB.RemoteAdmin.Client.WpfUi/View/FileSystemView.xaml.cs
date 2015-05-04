using System.Windows.Controls;

namespace JPB.RemoteAdmin.Client.WpfUi.View
{
    /// <summary>
    /// Interaction logic for FileSystemView.xaml
    /// </summary>
    public partial class FileSystemView : UserControl
    {
        public FileSystemView()
        {
            InitializeComponent();

            //treeView.PreviewerWebBrowser = this.PreviewerWebBrowser;
            //var activeXInstance = (SHDocVw.WebBrowser)PreviewerWebBrowser.ActiveXInstance;
            //activeXInstance.FileDownload += activeXInstance_FileDownload;

        }

        void activeXInstance_FileDownload(bool ActiveDocument, ref bool Cancel)
        {
            Cancel = true;
        }
    }
}
