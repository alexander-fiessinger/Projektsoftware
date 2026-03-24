using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Projektsoftware.Converters
{
    /// <summary>
    /// JSON converter for nullable decimal values that handles empty strings and invalid values
    /// </summary>
    public class NullableDecimalConverter : JsonConverter<decimal?>
    {
        public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[NullableDecimalConverter] Called with TokenType: {reader.TokenType}");

                if (reader.TokenType == JsonTokenType.Null)
                {
                    System.Diagnostics.Debug.WriteLine("[NullableDecimalConverter] Returning null for Null token");
                    return null;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    var stringValue = reader.GetString();
                    System.Diagnostics.Debug.WriteLine($"[NullableDecimalConverter] String value: '{stringValue}'");

                    // Handle empty strings
                    if (string.IsNullOrWhiteSpace(stringValue))
                    {
                        System.Diagnostics.Debug.WriteLine("[NullableDecimalConverter] Returning null for empty string");
                        return null;
                    }

                    // Try to parse the string using invariant culture (handles both "1.20" and "1,20")
                    if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                    {
                        System.Diagnostics.Debug.WriteLine($"[NullableDecimalConverter] Parsed string to: {result}");
                        return result;
                    }

                    // Invalid string value, return null
                    System.Diagnostics.Debug.WriteLine("[NullableDecimalConverter] Returning null for invalid string");
                    return null;
                }

                if (reader.TokenType == JsonTokenType.Number)
                {
                    // Try to get the decimal value
                    if (reader.TryGetDecimal(out var numValue))
                    {
                        System.Diagnostics.Debug.WriteLine($"[NullableDecimalConverter] Read number: {numValue}");
                        return numValue;
                    }

                    // Try to get as double and convert
                    if (reader.TryGetDouble(out var doubleValue))
                    {
                        var decimalValue = (decimal)doubleValue;
                        System.Diagnostics.Debug.WriteLine($"[NullableDecimalConverter] Read double and converted: {decimalValue}");
                        return decimalValue;
                    }

                    // Number is invalid, return null
                    System.Diagnostics.Debug.WriteLine("[NullableDecimalConverter] Returning null for invalid number");
                    return null;
                }

                // For any other token type, return null
                System.Diagnostics.Debug.WriteLine($"[NullableDecimalConverter] Returning null for unexpected token type: {reader.TokenType}");
                return null;
            }
            catch (Exception ex)
            {
                // If anything goes wrong, return null instead of throwing
                System.Diagnostics.Debug.WriteLine($"[NullableDecimalConverter] Exception: {ex.Message}");
                return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteNumberValue(value.Value);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
