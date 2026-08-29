using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Shared.ADT.Sponsors;

namespace Content.Server.ADT.Sponsors;

public static class SponsorSerialization
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

        options.Converters.Add(new SponsorColorJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }

    public static string SerializeBenefits(SponsorBenefits benefits)
    {
        return JsonSerializer.Serialize(benefits, Options);
    }

    public static bool TryDeserializeBenefits(string? json, out SponsorBenefits benefits)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            benefits = new SponsorBenefits();
            return true;
        }

        try
        {
            benefits = JsonSerializer.Deserialize<SponsorBenefits>(json, Options) ?? new SponsorBenefits();
            return true;
        }
        catch (JsonException)
        {
            benefits = new SponsorBenefits();
            return false;
        }
    }
}
