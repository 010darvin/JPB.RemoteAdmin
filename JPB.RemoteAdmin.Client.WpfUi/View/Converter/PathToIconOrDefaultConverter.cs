using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using JPB.RemoteAdmin.Client.WpfUi.ViewModel;
using JPB.RemoteAdmin.Common.Messages;

namespace JPB.RemoteAdmin.Client.WpfUi.View.Converter
{
    public class PathToIconOrDefaultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var fileModel = value as FileInfoViewModel;

            if (fileModel == null)
                return null;

            System.Windows.Media.ImageSource icon;

            if (File.Exists(fileModel.PathToTarget))
            {
                using (System.Drawing.Icon sysicon = System.Drawing.Icon.ExtractAssociatedIcon(fileModel.PathToTarget))
                {
                    icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                              sysicon.Handle,
                              System.Windows.Int32Rect.Empty,
                              System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
            }
            else
            {
                if (fileModel.Self.Icon == null || fileModel.Self.Icon.Length == 0)
                {
                    if (fileModel.Self.Type == FileType.File)
                    {
                        icon = new BitmapImage(new Uri("../../Resources/Images/1421025570_file-48.png", UriKind.Relative));
                    }
                    else
                    {
                        icon = new BitmapImage(new Uri("../../Resources/Images/1421025482_Folder.png", UriKind.Relative));
                    }
                }
                else
                {
                    icon = fileModel.Icon;
                }
            }

            return icon;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}