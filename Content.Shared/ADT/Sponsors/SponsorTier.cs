using System.Text.Json.Serialization;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Sponsors;

[Serializable, NetSerializable]
public sealed class SponsorTier
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("benefits")]
    public SponsorBenefits Benefits { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    public SponsorTier Clone()
    {
        return new SponsorTier
        {
            Id = Id,
            Name = Name,
            DisplayName = DisplayName,
            Description = Description,
            Priority = Priority,
            Enabled = Enabled,
            Benefits = Benefits.Clone(),
            CreatedAt = CreatedAt,
        };
    }
}
