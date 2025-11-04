using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppBackend.ApiCore.JsonConverters
{
    /// <summary>
    /// Custom JSON converter for DateOnly type
    /// Handles serialization/deserialization in ISO 8601 format: "YYYY-MM-DD"
    /// </summary>
    public class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        private const string DateFormat = "yyyy-MM-dd";

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    throw new JsonException("DateOnly value cannot be empty");
                }

                if (DateOnly.TryParseExact(stringValue, DateFormat, out var date))
                {
                    return date;
                }

                // Try parsing with different formats as fallback
                if (DateOnly.TryParse(stringValue, out var parsedDate))
                {
                    return parsedDate;
                }

                throw new JsonException($"Unable to parse '{stringValue}' to DateOnly. Expected format: {DateFormat} (e.g., '2025-06-19')");
            }

            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing DateOnly. Expected String with format: {DateFormat}");
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(DateFormat));
        }
    }

    /// <summary>
    /// Custom JSON converter for nullable DateOnly type
    /// </summary>
    public class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
    {
        private const string DateFormat = "yyyy-MM-dd";

        public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }

                if (DateOnly.TryParseExact(stringValue, DateFormat, out var date))
                {
                    return date;
                }

                // Try parsing with different formats as fallback
                if (DateOnly.TryParse(stringValue, out var parsedDate))
                {
                    return parsedDate;
                }

                throw new JsonException($"Unable to parse '{stringValue}' to DateOnly. Expected format: {DateFormat} (e.g., '2025-06-19')");
            }

            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing DateOnly?. Expected String with format: {DateFormat} or null");
        }

        public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.ToString(DateFormat));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
