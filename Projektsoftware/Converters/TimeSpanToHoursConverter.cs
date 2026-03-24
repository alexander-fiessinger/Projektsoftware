using System;
using System.Globalization;
using System.Windows.Data;

namespace Projektsoftware.Converters
{
    public class TimeSpanToHoursConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                return timeSpan.TotalHours.ToString("F2", CultureInfo.CurrentCulture);
            }
            return "0,00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && double.TryParse(str, NumberStyles.Any, CultureInfo.CurrentCulture, out double hours))
            {
                return TimeSpan.FromHours(hours);
            }
            return TimeSpan.Zero;
        }
    }
}
