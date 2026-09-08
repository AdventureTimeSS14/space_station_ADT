using System.Linq;
using System.Text.Json.Serialization;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Sponsors;

[Serializable, NetSerializable]
public sealed class SponsorBenefits
{
    #region Роли

    [JsonPropertyName("roleBypass")]
    public SponsorRoleBypass RoleBypass { get; set; } = SponsorRoleBypass.None;

    [JsonPropertyName("excludedDepartments")]
    public HashSet<string> ExcludedDepartments { get; set; } = new();

    [JsonPropertyName("excludedJobs")]
    public HashSet<string> ExcludedJobs { get; set; } = new();

    #endregion

    #region Кастомизация персонажа

    [JsonPropertyName("loadouts")]
    public HashSet<string> Loadouts { get; set; } = new();

    [JsonPropertyName("allLoadouts")]
    public bool AllLoadouts { get; set; }

    [JsonPropertyName("markings")]
    public HashSet<string> Markings { get; set; } = new();

    [JsonPropertyName("allMarkings")]
    public bool AllMarkings { get; set; }

    [JsonPropertyName("species")]
    public HashSet<string> Species { get; set; } = new();

    [JsonPropertyName("traits")]
    public HashSet<string> Traits { get; set; } = new();

    [JsonPropertyName("ttsVoices")]
    public HashSet<string> TtsVoices { get; set; } = new();

    [JsonPropertyName("allTtsVoices")]
    public bool AllTtsVoices { get; set; }

    #endregion

    #region Чат и призрак

    [JsonPropertyName("oocColor")]
    public Color? OocColor { get; set; }

    [JsonPropertyName("allowCustomOocColor")]
    public bool AllowCustomOocColor { get; set; }

    [JsonPropertyName("ghostColors")]
    public List<Color> GhostColors { get; set; } = new();

    [JsonPropertyName("allowCustomGhostColor")]
    public bool AllowCustomGhostColor { get; set; }

    #endregion

    #region Прочее

    [JsonPropertyName("discordRoles")]
    public HashSet<string> DiscordRoles { get; set; } = new();

    [JsonPropertyName("priorityJoin")]
    public bool PriorityJoin { get; set; }

    [JsonPropertyName("extraCharacterSlots")]
    public int ExtraCharacterSlots { get; set; }

    #endregion

    public SponsorBenefits Clone()
    {
        return new SponsorBenefits
        {
            RoleBypass = RoleBypass,
            ExcludedDepartments = new HashSet<string>(ExcludedDepartments),
            ExcludedJobs = new HashSet<string>(ExcludedJobs),
            Loadouts = new HashSet<string>(Loadouts),
            AllLoadouts = AllLoadouts,
            Markings = new HashSet<string>(Markings),
            AllMarkings = AllMarkings,
            Species = new HashSet<string>(Species),
            Traits = new HashSet<string>(Traits),
            TtsVoices = new HashSet<string>(TtsVoices),
            AllTtsVoices = AllTtsVoices,
            OocColor = OocColor,
            AllowCustomOocColor = AllowCustomOocColor,
            GhostColors = new List<Color>(GhostColors),
            AllowCustomGhostColor = AllowCustomGhostColor,
            DiscordRoles = new HashSet<string>(DiscordRoles),
            PriorityJoin = PriorityJoin,
            ExtraCharacterSlots = ExtraCharacterSlots,
        };
    }

    public static SponsorBenefits Merge(IReadOnlyList<SponsorBenefitLayer> layers)
    {
        var result = new SponsorBenefits();

        HashSet<string>? excludedDepartments = null;
        HashSet<string>? excludedJobs = null;

        foreach (var layer in layers.OrderBy(x => x.Priority))
        {
            var benefits = layer.Benefits;

            result.RoleBypass |= benefits.RoleBypass;

            if ((benefits.RoleBypass & SponsorRoleBypass.Jobs) != 0)
            {
                if (excludedDepartments == null)
                {
                    excludedDepartments = new HashSet<string>(benefits.ExcludedDepartments);
                    excludedJobs = new HashSet<string>(benefits.ExcludedJobs);
                }
                else
                {
                    excludedDepartments.IntersectWith(benefits.ExcludedDepartments);
                    excludedJobs!.IntersectWith(benefits.ExcludedJobs);
                }
            }

            result.Loadouts.UnionWith(benefits.Loadouts);
            result.AllLoadouts |= benefits.AllLoadouts;
            result.Markings.UnionWith(benefits.Markings);
            result.AllMarkings |= benefits.AllMarkings;
            result.Species.UnionWith(benefits.Species);
            result.Traits.UnionWith(benefits.Traits);
            result.TtsVoices.UnionWith(benefits.TtsVoices);
            result.AllTtsVoices |= benefits.AllTtsVoices;

            if (benefits.OocColor != null)
                result.OocColor = benefits.OocColor;

            result.AllowCustomOocColor |= benefits.AllowCustomOocColor;

            foreach (var color in benefits.GhostColors)
            {
                if (!result.GhostColors.Contains(color))
                    result.GhostColors.Add(color);
            }

            result.AllowCustomGhostColor |= benefits.AllowCustomGhostColor;
            result.DiscordRoles.UnionWith(benefits.DiscordRoles);
            result.PriorityJoin |= benefits.PriorityJoin;
            result.ExtraCharacterSlots = Math.Max(result.ExtraCharacterSlots, benefits.ExtraCharacterSlots);
        }

        if (excludedDepartments != null)
        {
            result.ExcludedDepartments = excludedDepartments;
            result.ExcludedJobs = excludedJobs!;
        }

        return result;
    }
}

public readonly record struct SponsorBenefitLayer(SponsorBenefits Benefits, int Priority);
