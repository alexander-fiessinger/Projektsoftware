using System;
using System.Globalization;
using System.Windows.Data;

namespace Projektsoftware.Converters
{
    public class EuroCurrencyConverter : IValueConverter
    {
        private static readonly CultureInfo euroFormat = new CultureInfo("de-DE");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return decimalValue.ToString("C", euroFormat);
            }
            
            if (value != null && decimal.TryParse(value.ToString(), out decimal parsedValue))
            {
                return parsedValue.ToString("C", euroFormat);
            }

            return "0,00 €";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0m;

            string stringValue = value.ToString()
                .Replace("€", "")
                .Replace(" ", "")
                .Trim();

            if (decimal.TryParse(stringValue, NumberStyles.Currency, euroFormat, out decimal result))
            {
                return result;
            }

            return 0m;
        }
    }
}
