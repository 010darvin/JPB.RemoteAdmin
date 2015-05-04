using System;
using System.Globalization;
using System.Windows.Data;

namespace JPB.RemoteAdmin.Client.WpfUi.View.Converter
{
    public class LongToHDDSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ToFuzzyByteString((long) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public string ToFuzzyByteString(long bytes)
        {
            double s = bytes;
            string[] format = new string[]
                  {
                      "{0} bytes", "{0} KB",  
                      "{0} MB", "{0} GB", "{0} TB", "{0} PB", "{0} EB"
                  };

            int i = 0;

            while (i < format.Length && s >= 1024)
            {
                s = (long)(100 * s / 1024) / 100.0;
                i++;
            }
            return string.Format(format[i], s);
        }
    }
}