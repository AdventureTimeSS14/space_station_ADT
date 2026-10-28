using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Prototypes;
using System.Linq;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.Examine;
using Content.Shared.Nutrition.Components;
using Content.Shared.Temperature;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Surgery;

public sealed partial class SurgerySystem
{
    private const float StrapSurfaceModifier = 1.25f;
    private const float FloorSurfaceModifier = 1.5f;
    private const float MinSuccessChance = 0.05f;

    private static readonly ProtoId<SurgeryToolQualityPrototype> DrapingQuality = "Draping";

    private readonly record struct SurgeryToolMatch(float Speed, float Success, List<SurgeryStepEffect> FailureEffects, SoundSpecifier? Sound = null);

    private static readonly SurgeryToolMatch ExactMatch = new(1f, 1f, new());

    private bool TryMatchTool(SurgeryStepEntry step, EntityUid item, out SurgeryToolMatch match)
    {
        match = ExactMatch;

        if (step.Quality is { } qualityId)
            return TryMatchQuality(qualityId, item, out match);

        return MatchesOrgan(step, item);
    }

    private bool TryMatchQuality(ProtoId<SurgeryToolQualityPrototype> qualityId, EntityUid item, out SurgeryToolMatch match)
    {
        match = ExactMatch;

        if (TryComp<SurgeryToolComponent>(item, out var tool) && GetToolQualities(item, tool).Contains(qualityId))
            return true;

        if (!PrototypeManager.TryIndex(qualityId, out var quality))
            return false;

        foreach (var entry in quality.Entries)
        {
            if (!EntryMatches(entry, item))
                continue;

            match = new SurgeryToolMatch(entry.Speed, entry.Success, entry.FailureEffects, entry.Sound);
            return true;
        }

        return false;
    }

    private List<ProtoId<SurgeryToolQualityPrototype>> GetToolQualities(EntityUid item, SurgeryToolComponent tool)
    {
        if (tool.ActiveQualities is { } active && _toggle.IsActivated(item))
            return active;

        return tool.ExtraQualities;
    }

    private void OnToolExamined(Entity<SurgeryToolComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var proper = GetProperQualities(ent.Owner);

        if (ent.Comp.ActiveQualities is not { } active)
        {
            var qualities = ent.Comp.ExtraQualities.Union(proper).ToList();
            if (qualities.Count > 0)
                args.PushMarkup(Loc.GetString("surgery-tool-examine-qualities", ("qualities", FormatQualities(qualities))));
            return;
        }

        var activated = _toggle.IsActivated(ent.Owner);
        var current = activated ? active : ent.Comp.ExtraQualities;
        var other = activated ? ent.Comp.ExtraQualities : active;

        args.PushMarkup(Loc.GetString("surgery-tool-examine-current", ("qualities", FormatQualities(current))));
        args.PushMarkup(Loc.GetString("surgery-tool-examine-switch", ("qualities", FormatQualities(other))));
    }

    private List<ProtoId<SurgeryToolQualityPrototype>> GetProperQualities(EntityUid item)
    {
        var result = new List<ProtoId<SurgeryToolQualityPrototype>>();

        foreach (var quality in PrototypeManager.EnumeratePrototypes<SurgeryToolQualityPrototype>())
        {
            foreach (var entry in quality.Entries)
            {
                if (entry.Whitelist == null || entry.Speed > 1f || entry.Success < 1f || !EntryMatches(entry, item))
                    continue;

                result.Add(quality.ID);
                break;
            }
        }

        return result;
    }

    private string FormatQualities(List<ProtoId<SurgeryToolQualityPrototype>> qualities)
    {
        var names = new List<string>();
        foreach (var id in qualities)
        {
            if (PrototypeManager.TryIndex(id, out var quality))
                names.Add(Loc.GetString(quality.Name));
        }

        return names.Count > 0 ? string.Join(", ", names) : Loc.GetString("surgery-tool-examine-none");
    }

    private bool EntryMatches(SurgeryToolQualityEntry entry, EntityUid item)
    {
        if (!entry.HasCriteria)
            return false;

        if (entry.Whitelist != null && !_whitelist.IsWhitelistPass(entry.Whitelist, item))
            return false;

        if (entry.ToolQuality is { } toolQuality && !_tool.HasQuality(item, toolQuality))
            return false;

        if (entry.Utensil != UtensilType.None
            && (!TryComp<UtensilComponent>(item, out var utensil) || (utensil.Types & entry.Utensil) == 0))
            return false;

        if (entry.RequireHot)
        {
            var hot = new IsHotEvent();
            RaiseLocalEvent(item, hot);
            if (!hot.IsHot)
                return false;
        }

        return true;
    }

    private bool MatchesOrgan(SurgeryStepEntry step, EntityUid item)
    {
        if (step.Organ is not { } category)
            return false;

        if (TryComp<OrganComponent>(item, out var organ) && organ.Category == category)
            return true;

        return _detachableOrgan.TryGetDetachedOrgan(item, out var detached)
               && TryComp<OrganComponent>(detached, out var detachedOrgan)
               && detachedOrgan.Category == category;
    }

    private TimeSpan GetStepDuration(EntityUid patient, SurgeryStepEntry step, SurgeryToolMatch match, EntityUid item)
    {
        var toolSpeed = CompOrNull<SurgeryToolComponent>(item)?.Speed ?? 1f;
        return step.Duration * (match.Speed * toolSpeed * GetSurfaceModifier(patient));
    }

    private SoundSpecifier? GetStepSound(SurgeryStepEntry step, SurgeryToolMatch match, EntityUid item)
    {
        return CompOrNull<SurgeryToolComponent>(item)?.Sound ?? match.Sound ?? step.Sound;
    }

    private float GetSurfaceModifier(EntityUid patient)
    {
        if (!TryComp<BuckleComponent>(patient, out var buckle) || buckle.BuckledTo is not { } strap)
            return FloorSurfaceModifier;

        if (TryComp<OperatingTableComponent>(strap, out var table))
            return table.Modifier;

        return StrapSurfaceModifier;
    }

    private bool IsSurgeonTrained(EntityUid? surgeon)
    {
        return HasComp<SurgerySkillComponent>(surgeon);
    }

    private float GetSuccessChance(EntityUid patient, EntityUid? surgeon, SurgeryStepEntry step, SurgeryToolMatch match)
    {
        var baseChance = IsSurgeonTrained(surgeon) ? 1f : step.SuccessChance;
        return Math.Clamp(baseChance * match.Success * GetAnesthesiaMultiplier(patient), MinSuccessChance, 1f);
    }
}
