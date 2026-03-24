using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Projektsoftware.Converters
{
    /// <summary>
    /// JSON converter for Easybill prices - API expects/returns values in cents
    /// Euro to Cent conversion: 120.50 € = 12050 cents
    /// </summary>
    public class EasybillPriceConverter : JsonConverter<decimal?>
    {
        public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[EasybillPriceConverter] Called with TokenType: {reader.TokenType}");

                if (reader.TokenType == JsonTokenType.Null)
                {
                    System.Diagnostics.Debug.WriteLine("[EasybillPriceConverter] Returning null for Null token");
                    return null;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    var stringValue = reader.GetString();
                    System.Diagnostics.Debug.WriteLine($"[EasybillPriceConverter] String value: '{stringValue}'");

                    if (string.IsNullOrWhiteSpace(stringValue))
                    {
                        return null;
                    }

                    if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var centValue))
                    {
                        // Konvertiere Cent zu Euro
                        var euroValue = centValue / 100m;
                        System.Diagnostics.Debug.WriteLine($"[EasybillPriceConverter] Converted {centValue} cents to {euroValue} EUR");
                        return euroValue;
                    }

                    return null;
                }

                if (reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetDecimal(out var centValue))
                    {
                        // Konvertiere Cent zu Euro
                        var euroValue = centValue / 100m;
                        System.Diagnostics.Debug.WriteLine($"[EasybillPriceConverter] Converted {centValue} cents to {euroValue} EUR");
                        return euroValue;
                    }

                    if (reader.TryGetDouble(out var doubleValue))
                    {
                        var centValueDouble = (decimal)doubleValue;
                        var euroValue = centValueDouble / 100m;
                        System.Diagnostics.Debug.WriteLine($"[EasybillPriceConverter] Converted {centValueDouble} cents to {euroValue} EUR");
                        return euroValue;
                    }

                    return null;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EasybillPriceConverter] Exception: {ex.Message}");
                return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                // Konvertiere Euro zu Cent für die API
                var centValue = value.Value * 100m;
                
                // Runde auf ganze Cent
                var roundedCents = Math.Round(centValue, 0, MidpointRounding.AwayFromZero);
                
                System.Diagnostics.Debug.WriteLine($"[EasybillPriceConverter] Writing {value.Value} EUR as {roundedCents} cents");
                
                writer.WriteNumberValue(roundedCents);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }

    /// <summary>
    /// JSON converter for non-nullable Easybill prices
    /// </summary>
    public class EasybillPriceConverterNotNullable : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                if (reader.TokenType == JsonTokenType.Null)
                    return 0m;

                if (reader.TokenType == JsonTokenType.String)
                {
                    var stringValue = reader.GetString();
                    if (string.IsNullOrWhiteSpace(stringValue))
                        return 0m;

                    if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var centValue))
                    {
                        return centValue / 100m; // Cent zu Euro
                    }
                    return 0m;
                }

                if (reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetDecimal(out var centValue))
                    {
                        return centValue / 100m; // Cent zu Euro
                    }

                    if (reader.TryGetDouble(out var doubleValue))
                    {
                        return (decimal)doubleValue / 100m;
                    }
                }

                return 0m;
            }
            catch
            {
                return 0m;
            }
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            // Konvertiere Euro zu Cent
            var centValue = Math.Round(value * 100m, 0, MidpointRounding.AwayFromZero);
            writer.WriteNumberValue(centValue);
        }
    }
}
