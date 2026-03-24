using System;
using System.Globalization;
using System.Windows.Data;

namespace Projektsoftware.Converters
{
    public class HourlyRateCalculatorConverter : IMultiValueConverter
    {
        private static readonly CultureInfo euroFormat = new CultureInfo("de-DE");

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return "0,00 €";

            try
            {
                // Erster Wert: TimeSpan (Duration)
                if (values[0] is TimeSpan duration)
                {
                    double hours = duration.TotalHours;

                    // Zweiter Wert: decimal (HourlyRate)
                    if (values[1] is decimal hourlyRate)
                    {
                        decimal amount = (decimal)hours * hourlyRate;
                        return amount.ToString("C", euroFormat);
                    }
                }
            }
            catch
            {
                return "0,00 €";
            }

            return "0,00 €";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
