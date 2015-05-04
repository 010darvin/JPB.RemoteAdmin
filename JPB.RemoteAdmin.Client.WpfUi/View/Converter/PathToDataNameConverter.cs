using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace JPB.RemoteAdmin.Client.WpfUi.View.Converter
{
    public class PathToDataNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var vst = value as string;
            if (vst == null || vst.LastIndexOf(@"\") + 1 == vst.Length)
            {
                return value;
            }

            return Path.GetFileName(vst);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}