using System;
using System.Globalization;
using System.Windows.Data;

namespace JPB.RemoteAdmin.Client.WpfUi.View.Converter
{
    public class HeaderConverter : IValueConverter
    {
        public const int Length = 10;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            if (str.Length > Length - 3)
            {
                return str.Substring(0, Length - 3) + "...";
            }
            return str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}