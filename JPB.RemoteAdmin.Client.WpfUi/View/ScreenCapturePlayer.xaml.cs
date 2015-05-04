using JPB.RemoteAdmin.Client.WpfUi.ViewModel.MainWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JPB.RemoteAdmin.Client.WpfUi.View
{
    /// <summary>
    /// Interaction logic for ScreenCapturePlayer.xaml
    /// </summary>
    public partial class ScreenCapturePlayer : UserControl
    {
        public ScreenCapturePlayer()
        {
            InitializeComponent();
        }

        private void OnMouseEvent(MouseButton changedButton, bool down)
        {
            var context = DataContext as ScreenCaptureViewModel;

            if (context != null)
            {
                var pos = Mouse.GetPosition(this.ImageSource);
                context.EmitMouseClick(changedButton, down, pos, this.ImageSource.ActualWidth, this.ImageSource.ActualHeight);
            }
        }

        private void ImageSource_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OnMouseEvent(e.ChangedButton, true);
        }

        private void ImageSource_KeyDown(object sender, KeyEventArgs e)
        {
            var context = DataContext as ScreenCaptureViewModel;

            if (context != null)
            {
                context.EmitKeybordAction(e.Key);
            }
            e.Handled = true;
        }

        private void ImageSource_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            OnMouseEvent(e.ChangedButton, false);
        }

        private void ImageSource_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            
        }
    }
}
