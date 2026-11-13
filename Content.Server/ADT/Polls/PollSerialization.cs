using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Shared.ADT.Polls;

namespace Content.Server.ADT.Polls;

public static class PollSerialization
{
    public static readonly JsonSerializerOptions Options = BuildOptions();

    private static JsonSerializerOptions BuildOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public static T Deserialize<T>(string? json) where T : new()
    {
        if (string.IsNullOrWhiteSpace(json))
            return new T();

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options) ?? new T();
        }
        catch (JsonException)
        {
            return new T();
        }
    }
}
