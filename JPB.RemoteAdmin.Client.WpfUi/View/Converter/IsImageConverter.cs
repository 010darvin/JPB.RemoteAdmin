using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace JPB.RemoteAdmin.Client.WpfUi.View.Converter
{
    public class IsImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;

            if (!File.Exists(path))
            {
                return Visibility.Collapsed;
            }
            try
            {
                var fromFile = Image.FromFile(path);
                return Visibility.Visible;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}