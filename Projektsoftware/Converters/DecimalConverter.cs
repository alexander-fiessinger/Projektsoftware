using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Projektsoftware.Converters
{
    /// <summary>
    /// JSON converter for decimal values that handles empty strings and invalid values
    /// </summary>
    public class DecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DecimalConverter] Called with TokenType: {reader.TokenType}");

                if (reader.TokenType == JsonTokenType.Null)
                {
                    System.Diagnostics.Debug.WriteLine("[DecimalConverter] Returning 0 for Null token");
                    return 0m;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    var stringValue = reader.GetString();
                    System.Diagnostics.Debug.WriteLine($"[DecimalConverter] String value: '{stringValue}'");

                    // Handle empty strings
                    if (string.IsNullOrWhiteSpace(stringValue))
                    {
                        System.Diagnostics.Debug.WriteLine("[DecimalConverter] Returning 0 for empty string");
                        return 0m;
                    }

                    // Try to parse the string using invariant culture (handles both "1.20" and "1,20")
                    if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                    {
                        System.Diagnostics.Debug.WriteLine($"[DecimalConverter] Parsed string to: {result}");
                        return result;
                    }

                    // Invalid string value, return 0
                    System.Diagnostics.Debug.WriteLine("[DecimalConverter] Returning 0 for invalid string");
                    return 0m;
                }

                if (reader.TokenType == JsonTokenType.Number)
                {
                    // Try to get the decimal value
                    if (reader.TryGetDecimal(out var numValue))
                    {
                        System.Diagnostics.Debug.WriteLine($"[DecimalConverter] Read number: {numValue}");
                        return numValue;
                    }

                    // Try to get as double and convert
                    if (reader.TryGetDouble(out var doubleValue))
                    {
                        var decimalValue = (decimal)doubleValue;
                        System.Diagnostics.Debug.WriteLine($"[DecimalConverter] Read double and converted: {decimalValue}");
                        return decimalValue;
                    }

                    // Number is invalid, return 0
                    System.Diagnostics.Debug.WriteLine("[DecimalConverter] Returning 0 for invalid number");
                    return 0m;
                }

                // For any other token type, return 0
                System.Diagnostics.Debug.WriteLine($"[DecimalConverter] Returning 0 for unexpected token type: {reader.TokenType}");
                return 0m;
            }
            catch (Exception ex)
            {
                // If anything goes wrong, return 0 instead of throwing
                System.Diagnostics.Debug.WriteLine($"[DecimalConverter] Exception: {ex.Message}");
                return 0m;
            }
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
