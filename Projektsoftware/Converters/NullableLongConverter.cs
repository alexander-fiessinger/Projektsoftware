using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Projektsoftware.Converters
{
    /// <summary>
    /// JSON converter for nullable long values that handles empty strings and invalid values
    /// </summary>
    public class NullableLongConverter : JsonConverter<long?>
    {
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[NullableLongConverter] Called with TokenType: {reader.TokenType}");

                if (reader.TokenType == JsonTokenType.Null)
                {
                    System.Diagnostics.Debug.WriteLine("[NullableLongConverter] Returning null for Null token");
                    return null;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    var stringValue = reader.GetString();
                    System.Diagnostics.Debug.WriteLine($"[NullableLongConverter] String value: '{stringValue}'");

                    // Handle empty strings
                    if (string.IsNullOrWhiteSpace(stringValue))
                    {
                        System.Diagnostics.Debug.WriteLine("[NullableLongConverter] Returning null for empty string");
                        return null;
                    }

                    // Try to parse the string
                    if (long.TryParse(stringValue, out var result))
                    {
                        System.Diagnostics.Debug.WriteLine($"[NullableLongConverter] Parsed string to: {result}");
                        return result;
                    }

                    // Invalid string value, return null
                    System.Diagnostics.Debug.WriteLine("[NullableLongConverter] Returning null for invalid string");
                    return null;
                }

                if (reader.TokenType == JsonTokenType.Number)
                {
                    // Use TryGetInt64 to safely handle numbers that might be too large or have decimals
                    if (reader.TryGetInt64(out var numValue))
                    {
                        System.Diagnostics.Debug.WriteLine($"[NullableLongConverter] Read number: {numValue}");
                        return numValue;
                    }

                    // Number is too large or is a decimal, return null
                    System.Diagnostics.Debug.WriteLine("[NullableLongConverter] Returning null for invalid number");
                    return null;
                }

                // For any other token type, return null
                System.Diagnostics.Debug.WriteLine($"[NullableLongConverter] Returning null for unexpected token type: {reader.TokenType}");
                return null;
            }
            catch (Exception ex)
            {
                // If anything goes wrong, return null instead of throwing
                System.Diagnostics.Debug.WriteLine($"[NullableLongConverter] Exception: {ex.Message}");
                return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
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
