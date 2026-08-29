using System.Text.Json.Serialization;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Sponsors;

[Serializable, NetSerializable]
public sealed class SponsorGrant
{
    public const int OverridePriorityBonus = 1000;

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("tierId")]
    public int? TierId { get; set; }

    [JsonPropertyName("tierName")]
    public string? TierName { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("overrides")]
    public SponsorBenefits? Overrides { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("createdBy")]
    public Guid? CreatedBy { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [JsonPropertyName("revoked")]
    public bool Revoked { get; set; }

    [JsonPropertyName("revokedAt")]
    public DateTime? RevokedAt { get; set; }

    [JsonPropertyName("revokedBy")]
    public Guid? RevokedBy { get; set; }

    /// <param name="nowUtc">Текущее время в UTC</param>
    public bool IsActive(DateTime nowUtc)
    {
        if (Revoked)
            return false;

        if (ExpiresAt != null && ExpiresAt.Value <= nowUtc)
            return false;

        return true;
    }

    public SponsorGrant Clone()
    {
        return new SponsorGrant
        {
            Id = Id,
            UserId = UserId,
            TierId = TierId,
            TierName = TierName,
            Priority = Priority,
            Overrides = Overrides?.Clone(),
            Comment = Comment,
            CreatedAt = CreatedAt,
            CreatedBy = CreatedBy,
            ExpiresAt = ExpiresAt,
            Revoked = Revoked,
            RevokedAt = RevokedAt,
            RevokedBy = RevokedBy,
        };
    }
}
