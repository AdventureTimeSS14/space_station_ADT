using System.Collections.Frozen;
using System.Linq;
using System.Text.Json.Serialization;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Sponsors;

public sealed class SponsorData
{
    public static readonly SponsorData Empty = FromBenefits(new SponsorBenefits(), null, Array.Empty<SponsorTierSummary>());

    public SponsorRoleBypass RoleBypass { get; private init; } = SponsorRoleBypass.None;
    public FrozenSet<string> ExcludedDepartments { get; private init; } = FrozenSet<string>.Empty;
    public FrozenSet<string> ExcludedJobs { get; private init; } = FrozenSet<string>.Empty;

    public FrozenSet<string> Loadouts { get; private init; } = FrozenSet<string>.Empty;
    public bool AllLoadouts { get; private init; }
    public FrozenSet<string> Markings { get; private init; } = FrozenSet<string>.Empty;
    public bool AllMarkings { get; private init; }
    public FrozenSet<string> Species { get; private init; } = FrozenSet<string>.Empty;
    public FrozenSet<string> Traits { get; private init; } = FrozenSet<string>.Empty;
    public FrozenSet<string> TtsVoices { get; private init; } = FrozenSet<string>.Empty;
    public bool AllTtsVoices { get; private init; }

    public Color? OocColor { get; private init; }
    public bool AllowCustomOocColor { get; private init; }
    public IReadOnlyList<Color> GhostColors { get; private init; } = Array.Empty<Color>();
    public bool AllowCustomGhostColor { get; private init; }

    public FrozenSet<string> DiscordRoles { get; private init; } = FrozenSet<string>.Empty;

    public bool PriorityJoin { get; private init; }
    public int ExtraCharacterSlots { get; private init; }

    public DateTime? NextExpiry { get; private init; }

    public IReadOnlyList<SponsorTierSummary> Tiers { get; private init; } = Array.Empty<SponsorTierSummary>();

    public bool HasAnyBenefit { get; private init; }

    public bool IsLoadoutAllowed(string loadoutId)
    {
        return AllLoadouts || Loadouts.Contains(loadoutId);
    }

    public bool IsMarkingAllowed(string markingId)
    {
        return AllMarkings || Markings.Contains(markingId);
    }

    public bool IsSpeciesAllowed(string speciesId)
    {
        return Species.Contains(speciesId);
    }

    public bool IsTraitAllowed(string traitId)
    {
        return Traits.Contains(traitId);
    }

    public bool IsTtsVoiceAllowed(string voiceId)
    {
        return AllTtsVoices || TtsVoices.Contains(voiceId);
    }

    public bool IsJobTimeBypassed(string jobId, string? departmentId)
    {
        if ((RoleBypass & SponsorRoleBypass.Jobs) == 0)
            return false;

        if (ExcludedJobs.Contains(jobId))
            return false;

        if (departmentId != null && ExcludedDepartments.Contains(departmentId))
            return false;

        return true;
    }

    public bool IsAntagTimeBypassed()
    {
        return (RoleBypass & SponsorRoleBypass.Antags) != 0;
    }

    public bool IsGhostColorAllowed(Color color)
    {
        if (AllowCustomGhostColor)
            return true;

        return GhostColors.Contains(color);
    }

    public static SponsorData FromBenefits(
        SponsorBenefits benefits,
        DateTime? nextExpiry,
        IReadOnlyList<SponsorTierSummary> tiers)
    {
        var hasAnyBenefit = benefits.RoleBypass != SponsorRoleBypass.None
                            || benefits.AllLoadouts
                            || benefits.AllMarkings
                            || benefits.Loadouts.Count > 0
                            || benefits.Markings.Count > 0
                            || benefits.Species.Count > 0
                            || benefits.Traits.Count > 0
                            || benefits.AllTtsVoices
                            || benefits.TtsVoices.Count > 0
                            || benefits.OocColor != null
                            || benefits.AllowCustomOocColor
                            || benefits.GhostColors.Count > 0
                            || benefits.AllowCustomGhostColor
                            || benefits.DiscordRoles.Count > 0
                            || benefits.PriorityJoin
                            || benefits.ExtraCharacterSlots > 0;

        return new SponsorData
        {
            RoleBypass = benefits.RoleBypass,
            ExcludedDepartments = benefits.ExcludedDepartments.ToFrozenSet(),
            ExcludedJobs = benefits.ExcludedJobs.ToFrozenSet(),
            Loadouts = benefits.Loadouts.ToFrozenSet(),
            AllLoadouts = benefits.AllLoadouts,
            Markings = benefits.Markings.ToFrozenSet(),
            AllMarkings = benefits.AllMarkings,
            Species = benefits.Species.ToFrozenSet(),
            Traits = benefits.Traits.ToFrozenSet(),
            TtsVoices = benefits.TtsVoices.ToFrozenSet(),
            AllTtsVoices = benefits.AllTtsVoices,
            OocColor = benefits.OocColor,
            AllowCustomOocColor = benefits.AllowCustomOocColor,
            GhostColors = benefits.GhostColors.ToArray(),
            AllowCustomGhostColor = benefits.AllowCustomGhostColor,
            DiscordRoles = benefits.DiscordRoles.ToFrozenSet(),
            PriorityJoin = benefits.PriorityJoin,
            ExtraCharacterSlots = benefits.ExtraCharacterSlots,
            NextExpiry = nextExpiry,
            Tiers = tiers,
            HasAnyBenefit = hasAnyBenefit,
        };
    }
}

[Serializable, NetSerializable]
public sealed class SponsorTierSummary
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }
}
