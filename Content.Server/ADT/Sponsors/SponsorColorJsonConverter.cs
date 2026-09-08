using System.Text.Json;
using System.Text.Json.Serialization;

namespace Content.Server.ADT.Sponsors;

public sealed class SponsorColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Ожидалась hex-строка цвета, получен {reader.TokenType}.");

        var raw = reader.GetString();

        if (raw == null)
            throw new JsonException("Ожидалась hex-строка цвета, получен null.");

        var color = Color.TryFromHex(raw);

        if (color == null)
            throw new JsonException($"'{raw}' не является корректным hex-цветом.");

        return color.Value;
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToHex());
    }
}
